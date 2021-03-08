using System;
using System.Globalization;

namespace SpeedCalc.Core.Runtime
{
    public sealed class Parser
    {
        enum Precedence
        {
            None,
            Assignment,  // =
            Or,          // or
            And,         // and
            Equality,    // == !=
            Comparison,  // < > <= >=
            Term,        // + -
            Factor,      // * /
            Exponent,    // **
            Unary,       // ! -
            Call,        // . () []
            Primary
        }

        delegate void ParseFn(bool canAssign);

        readonly struct Rule
        {
            public ParseFn Prefix { get; }

            public ParseFn Infix { get; }

            public Precedence Precedence { get; }

            public Rule(ParseFn prefix, ParseFn infix, Precedence precedence)
            {
                Prefix = prefix;
                Infix = infix;
                Precedence = precedence;
            }
        }

        readonly Rule[] ParseRules;

        State state;

        #region Utility Functions

        void PrintError(Token token, string message)
        {
            if (state.PanicMode)
                return;
            state.PanicMode = true;

            Console.Error.Write($"[line {token.Line}] Error");
            if (token.Type == TokenType.EOF)
                Console.Error.Write(" at end");
            else if (token.Type != TokenType.Error)
                Console.Error.Write($" at {token.Lexeme}");

            Console.Error.WriteLine($": {message}");
            state.HadError = true;
        }

        void Error(string message) => PrintError(state.Previous, message);

        void ErrorAtCurrent(string message) => PrintError(state.Current, message);

        void Advance()
        {
            state.Previous = state.Current;
            while (true)
            {
                state.Current = state.Scanner.ScanToken();
                if (state.Current.Type != TokenType.Error)
                    break;
                else
                    ErrorAtCurrent(state.Current.Lexeme);
            }
        }

        void Consume(TokenType tokenType, string message)
        {
            if (state.Current.Type == tokenType)
                Advance();
            else
                ErrorAtCurrent(message);
        }

        bool Check(TokenType type) => state.Current.Type == type;

        bool Match(TokenType type)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }

            return false;
        }

        void Synchronize()
        {
            state.PanicMode = false;

            while (state.Current.Type != TokenType.EOF)
            {
                if (state.Previous.Type == TokenType.Semicolon)
                    return;

                switch (state.Current.Type)
                {
                    case TokenType.Fn:
                    case TokenType.Var:
                    case TokenType.For:
                    case TokenType.If:
                    case TokenType.While:
                    case TokenType.Print:
                    case TokenType.Return:
                        return;

                    default:
                        Advance();
                        break;
                }
            }
        }

        Chunk CurrentChunk() => state.Compiler.Function.Chunk;

        byte MakeConstant(Value value)
        {
            var constantIndex = CurrentChunk().AddConstant(value);
            if (constantIndex > byte.MaxValue)
                Error("Too many constants in one chunk");
            return (byte)constantIndex;
        }

        int CurrentCodePosition() => CurrentChunk().Code.Count;

        void Emit(byte value) => CurrentChunk().Write(value, state.Previous.Line);

        void Emit(OpCode value) => Emit((byte)value);

        void Emit(OpCode op, byte arg)
        {
            Emit(op);
            Emit(arg);
        }

        void EmitReturn() => Emit(OpCode.Return);

        void EmitConstant(Value value) => Emit(OpCode.Constant, MakeConstant(value));

        int EmitJump(OpCode value)
        {
            Emit(value);
            Emit(0xff);
            Emit(0xff);
            return CurrentCodePosition() - 2;
        }

        void EmitLoop(int loopStart)
        {
            Emit(OpCode.Loop);

            var offset = CurrentChunk().Code.Count - loopStart + 2;
            if (offset > ushort.MaxValue)
                Error("Loop body too large");

            Emit((byte)((offset >> 8) & 0xff));
            Emit((byte)(offset & 0xff));
        }

        void PatchJump(int offset)
        {
            var jump = CurrentCodePosition() - offset - 2;
            if (jump > ushort.MaxValue)
                Error("Too much code to jump over");

            CurrentChunk().Code[offset] = (byte)((jump >> 8) & 0xff);
            CurrentChunk().Code[offset + 1] = (byte)(jump & 0xff);
        }

        void BeginScope()
        {
            state.Compiler.ScopeDepth++;
        }

        void EndScope()
        {
            state.Compiler.ScopeDepth--;

            var originalLocalCount = state.Compiler.LocalCount;
            while (state.Compiler.LocalCount > 0 && state.Compiler.Locals[state.Compiler.LocalCount - 1].Depth > state.Compiler.ScopeDepth)
                state.Compiler.LocalCount--;

            var discardedLocals = originalLocalCount - state.Compiler.LocalCount;
            if (discardedLocals == 1)
                Emit(OpCode.Pop);
            else if (discardedLocals > 1)
                Emit(OpCode.PopN, (byte)discardedLocals);
        }

        #endregion

        #region Parsing Functions

        Rule GetRule(TokenType tokenType) => ParseRules[(int)tokenType];

        void ParsePrecedence(Precedence precedence)
        {
            Advance();
            var prefixRule = GetRule(state.Previous.Type).Prefix;
            if (prefixRule is null)
                Error("Expect expression");
            else
            {
                var canAssign = precedence <= Precedence.Assignment;
                prefixRule(canAssign);

                while (precedence <= GetRule(state.Current.Type).Precedence)
                {
                    Advance();
                    var infixRule = GetRule(state.Previous.Type).Infix;
                    infixRule(canAssign);
                }

                if (canAssign && Match(TokenType.Equal))
                    Error("Invalid assignment target");
            }
        }

        int ResolveLocal(Token name)
        {
            for (int i = state.Compiler.LocalCount - 1; i >= 0; i--)
            {
                if (state.Compiler.Locals[i].Token.Lexeme == name.Lexeme)
                {
                    if (state.Compiler.Locals[i].Depth == -1)
                        Error("Cannot read variable in its own initializer");
                    return i;
                }
            }
            return -1;
        }

        byte IdentifierConstant(Token name)
        {
            return MakeConstant(Values.String(name.Lexeme));
        }

        void DeclareVariable()
        {
            if (state.Compiler.ScopeDepth == 0)
                return;

            var name = state.Previous;
            for (int i = state.Compiler.LocalCount - 1; i >= 0; i--)
            {
                var local = state.Compiler.Locals[i];
                if (local.Depth != -1 && local.Depth < state.Compiler.ScopeDepth)
                    break;

                if (name.Lexeme == local.Token.Lexeme)
                    Error("Variable with this name already defined in this scope");
            }

            state.Compiler.AddLocal(state, name);
        }

        byte ParseVariable(string message)
        {
            Consume(TokenType.Identifier, message);

            DeclareVariable();
            if (state.Compiler.ScopeDepth > 0)
                return 0;

            return IdentifierConstant(state.Previous);
        }

        void MarkInitialized() => state.Compiler.Locals[state.Compiler.LocalCount - 1].Depth = state.Compiler.ScopeDepth;

        void DefineVariable(byte global)
        {
            if (state.Compiler.ScopeDepth > 0)
            {
                MarkInitialized();
                return;
            }

            Emit(OpCode.DefineGlobal, global);
        }

        void And(bool canAssign)
        {
            var endJump = EmitJump(OpCode.JumpIfFalse);

            Emit(OpCode.Pop);
            ParsePrecedence(Precedence.And);

            PatchJump(endJump);
        }

        void Or(bool canAssign)
        {
            var elseJump = EmitJump(OpCode.JumpIfFalse);
            var endJump = EmitJump(OpCode.Jump);

            PatchJump(elseJump);
            Emit(OpCode.Pop);

            ParsePrecedence(Precedence.Or);
            PatchJump(endJump);
        }

        void Number(bool canAssign)
        {
            var value = decimal.Parse(state.Previous.Lexeme, CultureInfo.InvariantCulture);
            EmitConstant(Values.Number(value));
        }

        void Unary(bool canAssign)
        {
            var operatorType = state.Previous.Type;

            ParsePrecedence(Precedence.Unary);

            switch (operatorType)
            {
                case TokenType.Bang:
                    Emit(OpCode.Not);
                    break;
                case TokenType.Minus:
                    Emit(OpCode.Negate);
                    break;

                default:
                    return;
            }
        }

        void Binary(bool canAssign)
        {
            var operatorType = state.Previous.Type;

            var rule = GetRule(operatorType);
            ParsePrecedence(rule.Precedence + 1);

            switch (operatorType)
            {
                case TokenType.Minus:
                    Emit(OpCode.Subtract);
                    break;
                case TokenType.Plus:
                    Emit(OpCode.Add);
                    break;
                case TokenType.Slash:
                    Emit(OpCode.Divide);
                    break;
                case TokenType.BangEqual:
                    Emit(OpCode.Equal);
                    Emit(OpCode.Not);
                    break;
                case TokenType.EqualEqual:
                    Emit(OpCode.Equal);
                    break;
                case TokenType.LessEqual:
                    Emit(OpCode.Greater);
                    Emit(OpCode.Not);
                    break;
                case TokenType.Greater:
                    Emit(OpCode.Greater);
                    break;
                case TokenType.GreaterEqual:
                    Emit(OpCode.Less);
                    Emit(OpCode.Not);
                    break;
                case TokenType.Less:
                    Emit(OpCode.Less);
                    break;
                case TokenType.Star:
                    Emit(OpCode.Multiply);
                    break;
                case TokenType.StarStar:
                    Emit(OpCode.Exp);
                    break;
                case TokenType.Mod:
                    Emit(OpCode.Modulo);
                    break;

                default:
                    return;
            }
        }

        void Literal(bool canAssign)
        {
            switch (state.Previous.Type)
            {
                case TokenType.True:
                    Emit(OpCode.True);
                    break;
                case TokenType.False:
                    Emit((byte)OpCode.False);
                    break;
            }
        }

        void NamedVariable(Token name, bool canAssign)
        {
            OpCode get, set;
            var arg = ResolveLocal(name);
            if (arg != -1)
            {
                get = OpCode.LoadLocal;
                set = OpCode.AssignLocal;
            }
            else
            {
                arg = IdentifierConstant(name);
                get = OpCode.LoadGlobal;
                set = OpCode.AssignGlobal;
            }

            if (canAssign)
            {
                void EmitArithmeticAssignmentOperator(OpCode operation)
                {
                    Emit(get, (byte)arg);
                    Expression();
                    Emit(operation);
                    Emit(set, (byte)arg);
                }

                if (Match(TokenType.Equal))
                {
                    Expression();
                    Emit(set, (byte)arg);
                }
                else if (Match(TokenType.MinusEqual))
                    EmitArithmeticAssignmentOperator(OpCode.Subtract);
                else if (Match(TokenType.PlusEqual))
                    EmitArithmeticAssignmentOperator(OpCode.Add);
                else if (Match(TokenType.SlashEqual))
                    EmitArithmeticAssignmentOperator(OpCode.Divide);
                else if (Match(TokenType.StarEqual))
                    EmitArithmeticAssignmentOperator(OpCode.Multiply);
                else if (Match(TokenType.StarStarEqual))
                    EmitArithmeticAssignmentOperator(OpCode.Exp);
                else
                    Emit(get, (byte)arg);
            }
            else
                Emit(get, (byte)arg);
        }

        void Variable(bool canAssign)
        {
            NamedVariable(state.Previous, canAssign);
        }

        void Grouping(bool canAssign)
        {
            Expression();
            Consume(TokenType.ParenRight, "Expect ')' after expression");
        }

        void Expression()
        {
            ParsePrecedence(Precedence.Assignment);
        }

        void Block()
        {
            while (!Check(TokenType.BraceRight) && !Check(TokenType.EOF))
                Declaration();
            Consume(TokenType.BraceRight, "Expect '}' after block");
        }

        void ExpressionStatement()
        {
            Expression();
            Consume(TokenType.Semicolon, "Expect ';' after expression");
            Emit(OpCode.Pop);
        }

        void ForStatement()
        {
            BeginScope();

            // Initializer
            if (Match(TokenType.Var))
                VarDeclaration();
            else if (Match(TokenType.Semicolon))
            {
                // No initializer
            }
            else
                ExpressionStatement();

            var surroundingLoopState = state.InnermostLoop;
            state.InnermostLoop = LoopState.FromCurrentState(state);

            // Condition
            var exitJump = -1;
            if (!Match(TokenType.Semicolon))
            {
                Expression();
                Consume(TokenType.Semicolon, "Expect ';' after loop condition");
                // Jump out if condition is negative
                exitJump = EmitJump(OpCode.JumpIfFalse);
                Emit(OpCode.Pop); // Condition
            }

            // Increment
            if (!Match(TokenType.Colon))
            {
                var bodyJump = EmitJump(OpCode.Jump);

                var incrementStart = CurrentCodePosition();
                Expression();
                Emit(OpCode.Pop);
                Consume(TokenType.Colon, "Expect ':' after for clauses");

                EmitLoop(state.InnermostLoop.Start);
                state.InnermostLoop.Start = incrementStart;
                PatchJump(bodyJump);
            }

            Statement();

            EmitLoop(state.InnermostLoop.Start);

            if (exitJump != -1)
            {
                PatchJump(exitJump);
                Emit(OpCode.Pop); // Condition, if Pop has been jumped over above
            }

            state.InnermostLoop.PatchUnresolvedBreaks(state);
            state.InnermostLoop = surroundingLoopState;

            EndScope();
        }

        void IfStatement()
        {
            Expression();
            Consume(TokenType.Colon, "Expect ':' after condition");

            var thenJump = EmitJump(OpCode.JumpIfFalse);
            Emit(OpCode.Pop);
            Statement();
            var elseJump = EmitJump(OpCode.Jump);

            PatchJump(thenJump);
            Emit(OpCode.Pop);

            if (Match(TokenType.Else))
            {
                Consume(TokenType.Colon, "Expect ':' after else");
                Statement();
            }

            PatchJump(elseJump);
        }

        void PrintStatement()
        {
            Expression();
            Consume(TokenType.Semicolon, "Expect ';' after value");
            Emit(OpCode.Print);
        }

        void WhileStatement()
        {
            var surroundingLoopState = state.InnermostLoop;
            state.InnermostLoop = LoopState.FromCurrentState(state);

            Expression();
            Consume(TokenType.Colon, "Expect ':' after condition");

            var exitJump = EmitJump(OpCode.JumpIfFalse);

            Emit(OpCode.Pop);
            Statement();

            EmitLoop(state.InnermostLoop.Start);

            PatchJump(exitJump);
            Emit(OpCode.Pop);

            state.InnermostLoop.PatchUnresolvedBreaks(state);
            state.InnermostLoop = surroundingLoopState;
        }

        void ContinueStatement()
        {
            if (state.InnermostLoop.Start == -1)
                Error("Can't use continue outside of a loop");

            Consume(TokenType.Semicolon, "Expect ';' after continue");

            // Discard locals
            var popCount = 0;
            for (var i = state.Compiler.LocalCount - 1; i >= 0 && state.Compiler.Locals[i].Depth > state.InnermostLoop.ScopeDepth; i--)
                popCount++;
            if (popCount > 0)
                Emit(OpCode.PopN, (byte)popCount);

            EmitLoop(state.InnermostLoop.Start);
        }

        void BreakStatement()
        {
            if (state.InnermostLoop.Start == -1)
                Error("Can't use break outside of a loop");

            Consume(TokenType.Semicolon, "Expect ';' after break");

            // Discard locals
            var popCount = 0;
            for (var i = state.Compiler.LocalCount - 1; i >= 0 && state.Compiler.Locals[i].Depth > state.InnermostLoop.ScopeDepth; i--)
                popCount++;
            if (popCount > 0)
                Emit(OpCode.PopN, (byte)popCount);

            var breakJump = EmitJump(OpCode.Jump);
            state.InnermostLoop.UnresolvedBreaks.Write(breakJump);
        }

        void Statement()
        {
            if (Match(TokenType.Print))
                PrintStatement();
            else if (Match(TokenType.For))
                ForStatement();
            else if (Match(TokenType.If))
                IfStatement();
            else if (Match(TokenType.While))
                WhileStatement();
            else if (Match(TokenType.Continue))
                ContinueStatement();
            else if (Match(TokenType.Break))
                BreakStatement();
            else if (Match(TokenType.BraceLeft))
            {
                BeginScope();
                Block();
                EndScope();
            }
            else
                ExpressionStatement();
        }

        void VarDeclaration()
        {
            var global = ParseVariable("Expect variable name");

            Consume(TokenType.Equal, "Expect '=' after variable name");
            Expression();
            Consume(TokenType.Semicolon, "Expect ';' after variable declaration");
            DefineVariable(global);
        }

        void Declaration()
        {
            if (Match(TokenType.Var))
                VarDeclaration();
            else
                Statement();

            if (state.PanicMode)
                Synchronize();
        }

        #endregion

        Function EndCompile()
        {
            EmitReturn();

#if DEBUG
            if (!state.HadError)
            {
                foreach (var line in state.Compiler.Function.DisassembleFunction())
                    Console.WriteLine(line);
            }
#endif

            return state.Compiler.Function;
        }

        public Function Compile(string source)
        {
            state = new State
            {
                Scanner = new Scanner(source ?? throw new ArgumentNullException(nameof(source))),
                Compiler = new Compiler(FunctionType.Script),
            };

            Advance();

            while (!Match(TokenType.EOF))
                Declaration();

            var function = EndCompile();
            return state.HadError ? null : function;
        }

        public Parser()
        {
            ParseRules = new[]
            {
                new Rule(null,     null,   Precedence.None),       // Error
                new Rule(null,     null,   Precedence.None),       // EOF
                new Rule(Grouping, null,   Precedence.None),       // ParenLeft
                new Rule(null,     null,   Precedence.None),       // ParenRight
                new Rule(null,     null,   Precedence.None),       // BraceLeft
                new Rule(null,     null,   Precedence.None),       // BraceRight
                new Rule(null,     null,   Precedence.None),       // Comma
                new Rule(null,     null,   Precedence.None),       // Dot
                new Rule(null,     null,   Precedence.None),       // Colon
                new Rule(null,     null,   Precedence.None),       // Semicolon
                new Rule(Unary,    null,   Precedence.None),       // Bang
                new Rule(null,     Binary, Precedence.Equality),   // BangEqual
                new Rule(null,     null,   Precedence.None),       // Equal
                new Rule(null,     Binary, Precedence.Equality),   // EqualEqual
                new Rule(null,     Binary, Precedence.Comparison), // Greater
                new Rule(null,     Binary, Precedence.Comparison), // GreaterEqual
                new Rule(null,     Binary, Precedence.Comparison), // Less
                new Rule(null,     Binary, Precedence.Comparison), // LessEqual
                new Rule(Unary,    Binary, Precedence.Term),       // Minus
                new Rule(null,     null,   Precedence.None),       // MinusEqual
                new Rule(null,     Binary, Precedence.Term),       // Plus
                new Rule(null,     null,   Precedence.None),       // PlusEqual
                new Rule(null,     Binary, Precedence.Factor),     // Slash
                new Rule(null,     null,   Precedence.None),       // SlashEqual
                new Rule(null,     Binary, Precedence.Factor),     // Star
                new Rule(null,     null,   Precedence.None),       // StarEqual
                new Rule(null,     Binary, Precedence.Exponent),   // StarStar
                new Rule(null,     null,   Precedence.None),       // StarStarEqual
                new Rule(Variable, null,   Precedence.None),       // Identifier
                new Rule(Number,   null,   Precedence.None),       // Number
                new Rule(null,     And,    Precedence.And),        // And
                new Rule(null,     null,   Precedence.None),       // Break
                new Rule(null,     null,   Precedence.None),       // Continue
                new Rule(null,     null,   Precedence.None),       // Else
                new Rule(Literal,  null,   Precedence.None),       // False
                new Rule(null,     null,   Precedence.None),       // Fn,
                new Rule(null,     null,   Precedence.None),       // For
                new Rule(null,     null,   Precedence.None),       // If
                new Rule(null,     Binary, Precedence.Factor),     // Mod
                new Rule(null,     Or,     Precedence.Or),         // Or
                new Rule(null,     null,   Precedence.None),       // Print
                new Rule(null,     null,   Precedence.None),       // Return
                new Rule(Literal,  null,   Precedence.None),       // True
                new Rule(null,     null,   Precedence.None),       // Var
                new Rule(null,     null,   Precedence.None),       // While
            };
        }
    }
}
