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

        sealed class LoopState
        {
            public int Start { get; set; }

            public int ScopeDepth { get; }

            public RuntimeArray<int> UnresolvedBreaks { get; }

            public void PatchUnresolvedBreaks(Parser parser)
            {
                foreach (var breakJump in UnresolvedBreaks)
                    parser.PatchJump(breakJump);
            }

            LoopState(int loopStart, int scopeDepth, RuntimeArray<int> unresolvedBreaks)
            {
                Start = loopStart;
                ScopeDepth = scopeDepth;
                UnresolvedBreaks = unresolvedBreaks ?? throw new ArgumentNullException(nameof(unresolvedBreaks));
            }

            public static LoopState Default => new LoopState(-1, 0, new RuntimeArray<int>());

            public static LoopState FromCurrentState(Parser parser) => new LoopState(parser.CurrentCodePosition(), parser.Compiler.ScopeDepth, new RuntimeArray<int>());
        }

        readonly Rule[] ParseRules;

        Scanner Scanner { get; set; }

        Compiler Compiler { get; set; }

        Token Current { get; set; }

        Token Previous { get; set; }

        LoopState InnermostLoop { get; set; } = LoopState.Default;

        bool HadError { get; set; }

        bool PanicMode { get; set; }

        #region Utility Functions

        void PrintError(Token token, string message)
        {
            if (PanicMode)
                return;
            PanicMode = true;

            Console.Error.Write($"[line {token.Line}] Error");
            if (token.Type == TokenType.EOF)
                Console.Error.Write(" at end");
            else if (token.Type != TokenType.Error)
                Console.Error.Write($" at {token.Lexeme}");

            Console.Error.WriteLine($": {message}");
            HadError = true;
        }

        void Error(string message) => PrintError(Previous, message);

        void ErrorAtCurrent(string message) => PrintError(Current, message);

        void Advance()
        {
            Previous = Current;
            while (true)
            {
                Current = Scanner.ScanToken();
                if (Current.Type != TokenType.Error)
                    break;
                else
                    ErrorAtCurrent(Current.Lexeme);
            }
        }

        void Consume(TokenType tokenType, string message)
        {
            if (Current.Type == tokenType)
                Advance();
            else
                ErrorAtCurrent(message);
        }

        bool Check(TokenType type) => Current.Type == type;

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
            PanicMode = false;

            while (Current.Type != TokenType.EOF)
            {
                if (Previous.Type == TokenType.Semicolon)
                    return;

                switch (Current.Type)
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

        Chunk CurrentChunk() => Compiler.Function.Chunk;

        int CurrentCodePosition() => CurrentChunk().Code.Count;

        byte MakeConstant(Value value)
        {
            var constantIndex = CurrentChunk().AddConstant(value);
            if (constantIndex > byte.MaxValue)
                Error("Too many constants in one chunk");
            return (byte)constantIndex;
        }

        void Emit(byte value) => CurrentChunk().Write(value, Previous.Line);

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

        void BeginScope() => Compiler.BeginScope();

        void EndScope()
        {
            var discardedLocals = Compiler.EndScope();
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
            var prefixRule = GetRule(Previous.Type).Prefix;
            if (prefixRule is null)
                Error("Expect expression");
            else
            {
                var canAssign = precedence <= Precedence.Assignment;
                prefixRule(canAssign);

                while (precedence <= GetRule(Current.Type).Precedence)
                {
                    Advance();
                    var infixRule = GetRule(Previous.Type).Infix;
                    infixRule(canAssign);
                }

                if (canAssign && Match(TokenType.Equal))
                    Error("Invalid assignment target");
            }
        }

        int ResolveLocal(Token name)
        {
            for (int i = Compiler.LocalCount - 1; i >= 0; i--)
            {
                if (Compiler.Locals[i].Token.Lexeme == name.Lexeme)
                {
                    if (Compiler.Locals[i].Depth == -1)
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
            if (Compiler.ScopeDepth == 0)
                return;

            var name = Previous;
            for (int i = Compiler.LocalCount - 1; i >= 0; i--)
            {
                var local = Compiler.Locals[i];
                if (local.Depth != -1 && local.Depth < Compiler.ScopeDepth)
                    break;

                if (name.Lexeme == local.Token.Lexeme)
                    Error("Variable with this name already defined in this scope");
            }

            Compiler.AddLocal(Error, name);
        }

        byte ParseVariable(string message)
        {
            Consume(TokenType.Identifier, message);

            DeclareVariable();
            if (Compiler.ScopeDepth > 0)
                return 0;

            return IdentifierConstant(Previous);
        }

        void MarkInitialized()
        {
            if (Compiler.ScopeDepth == 0)
                return;
            Compiler.Locals[Compiler.LocalCount - 1].Depth = Compiler.ScopeDepth;
        }

        void DefineVariable(byte global)
        {
            if (Compiler.ScopeDepth > 0)
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
            var value = decimal.Parse(Previous.Lexeme, CultureInfo.InvariantCulture);
            EmitConstant(Values.Number(value));
        }

        void Unary(bool canAssign)
        {
            var operatorType = Previous.Type;

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
            var operatorType = Previous.Type;

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
            switch (Previous.Type)
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
            NamedVariable(Previous, canAssign);
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

            var surroundingLoopState = InnermostLoop;
            InnermostLoop = LoopState.FromCurrentState(this);

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

                EmitLoop(InnermostLoop.Start);
                InnermostLoop.Start = incrementStart;
                PatchJump(bodyJump);
            }

            Statement();

            EmitLoop(InnermostLoop.Start);

            if (exitJump != -1)
            {
                PatchJump(exitJump);
                Emit(OpCode.Pop); // Condition, if Pop has been jumped over above
            }

            InnermostLoop.PatchUnresolvedBreaks(this);
            InnermostLoop = surroundingLoopState;

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
            var surroundingLoopState = InnermostLoop;
            InnermostLoop = LoopState.FromCurrentState(this);

            Expression();
            Consume(TokenType.Colon, "Expect ':' after condition");

            var exitJump = EmitJump(OpCode.JumpIfFalse);

            Emit(OpCode.Pop);
            Statement();

            EmitLoop(InnermostLoop.Start);

            PatchJump(exitJump);
            Emit(OpCode.Pop);

            InnermostLoop.PatchUnresolvedBreaks(this);
            InnermostLoop = surroundingLoopState;
        }

        void ContinueStatement()
        {
            if (InnermostLoop.Start == -1)
                Error("Can't use continue outside of a loop");

            Consume(TokenType.Semicolon, "Expect ';' after continue");

            // Discard locals
            var popCount = 0;
            for (var i = Compiler.LocalCount - 1; i >= 0 && Compiler.Locals[i].Depth > InnermostLoop.ScopeDepth; i--)
                popCount++;
            if (popCount > 0)
                Emit(OpCode.PopN, (byte)popCount);

            EmitLoop(InnermostLoop.Start);
        }

        void BreakStatement()
        {
            if (InnermostLoop.Start == -1)
                Error("Can't use break outside of a loop");

            Consume(TokenType.Semicolon, "Expect ';' after break");

            // Discard locals
            var popCount = 0;
            for (var i = Compiler.LocalCount - 1; i >= 0 && Compiler.Locals[i].Depth > InnermostLoop.ScopeDepth; i--)
                popCount++;
            if (popCount > 0)
                Emit(OpCode.PopN, (byte)popCount);

            var breakJump = EmitJump(OpCode.Jump);
            InnermostLoop.UnresolvedBreaks.Write(breakJump);
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

        void Function(FunctionType functionType)
        {
            Compiler = new Compiler(Compiler, functionType, Previous.Lexeme);
            Compiler.BeginScope();

            Consume(TokenType.ParenLeft, "Expect '(' after function name");
            Consume(TokenType.ParenRight, "Expect ')' after parameters");

            Consume(TokenType.BraceLeft, "Expect '{' before function body");
            Block();

            var function = EndCompile();
            EmitConstant(Values.Function(function));
        }

        void FunctionDeclaration()
        {
            var global = ParseVariable("Expect function name");
            MarkInitialized();
            Function(FunctionType.Function);
            DefineVariable(global);
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
            if (Match(TokenType.Fn))
                FunctionDeclaration();
            else if (Match(TokenType.Var))
                VarDeclaration();
            else
                Statement();

            if (PanicMode)
                Synchronize();
        }

        #endregion

        Function EndCompile()
        {
            EmitReturn();

#if DEBUG
            if (!HadError)
            {
                foreach (var line in Compiler.Function.DisassembleFunction())
                    Console.WriteLine(line);
            }
#endif

            return Compiler.Function;
        }

        public Function Compile(string source)
        {
            Scanner = new Scanner(source ?? throw new ArgumentNullException(nameof(source)));
            Compiler = new Compiler(null, FunctionType.Script);

            Advance();

            while (!Match(TokenType.EOF))
                Declaration();

            var function = EndCompile();
            return HadError ? null : function;
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
