﻿using System;
using System.Globalization;

namespace SpeedCalc.Core.Runtime
{
    public enum Precedence
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

    public static class Parser
    {
        public struct Local
        {
            public Token Token;
            public int Depth;
        }

        public enum FunctionType
        {
            Function,
            Script,
        }

        public sealed class Compiler
        {
            public Function Function { get; set; }

            public FunctionType FunctionType { get;set; }

            public Local[] Locals { get; } = new Local[byte.MaxValue + 1];
            
            public int LocalCount { get; set; }

            public int ScopeDepth { get; set; }

            public void AddLocal(State state, Token name)
            {
                if (LocalCount == byte.MaxValue)
                    Error(state, "Too many locals in function");

                var index = LocalCount++;
                Locals[index].Token = name;
                Locals[index].Depth = -1;
            }

            public Compiler(FunctionType type)
            {
                FunctionType = type;
                Function = new Function(string.Empty, 0);

                var local = Locals[LocalCount++];
                local.Depth = 0;
                local.Token = new Token(TokenType.Error, string.Empty, 0);
            }
        }

        public sealed class LoopState
        {
            public int Start { get; set; }

            public int ScopeDepth { get; set; }

            public RuntimeArray<int> UnresolvedBreaks { get; set; }

            public void PatchUnresolvedBreaks(State state)
            {
                foreach (var breakJump in UnresolvedBreaks)
                    PatchJump(state, breakJump);
            }

            public LoopState(int loopStart, int scopeDepth, RuntimeArray<int> unresolvedBreaks)
            {
                Start = loopStart;
                ScopeDepth = scopeDepth;
                UnresolvedBreaks = unresolvedBreaks ?? throw new ArgumentNullException(nameof(unresolvedBreaks));
            }

            public static LoopState Default => new LoopState(-1, 0, new RuntimeArray<int>());

            public static LoopState FromCurrentState(State state) => new LoopState(CurrentCodePosition(state), state.Compiler.ScopeDepth, new RuntimeArray<int>());
        }

        public sealed class State
        {
            public Scanner Scanner { get; set; }

            public Compiler Compiler { get; set; }

            public Token Current { get; set; }

            public Token Previous { get; set; }

            public LoopState InnermostLoop { get; set; } = LoopState.Default;

            public bool HadError { get; set; }

            public bool PanicMode { get; set; }
        }

        delegate void ParseFn(State state, bool canAssign);

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

        static readonly Rule[] ParseRules = new[]
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

        static Rule GetRule(TokenType tokenType) => ParseRules[(int)tokenType];

        #region Utility Functions

        static void PrintError(State state, Token token, string message)
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

        static void Error(State state, string message)
        {
            PrintError(state, state.Previous, message);
        }

        static void ErrorAtCurrent(State state, string message)
        {
            PrintError(state, state.Current, message);
        }

        static void Advance(State state)
        {
            state.Previous = state.Current;
            while (true)
            {
                state.Current = state.Scanner.ScanToken();
                if (state.Current.Type != TokenType.Error)
                    break;
                else
                    ErrorAtCurrent(state, state.Current.Lexeme);
            }
        }

        static void Consume(State state, TokenType tokenType, string message)
        {
            if (state.Current.Type == tokenType)
                Advance(state);
            else
                ErrorAtCurrent(state, message);
        }

        static bool Check(State state, TokenType type) => state.Current.Type == type;

        static bool Match(State state, TokenType type)
        {
            if (!Check(state, type))
                return false;
            Advance(state);
            return true;
        }

        static void Synchronize(State state)
        {
            state.PanicMode = false;

            while(state.Current.Type != TokenType.EOF)
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
                        Advance(state);
                        break;
                }
            }
        }

        static Chunk CurrentChunk(State state) => state.Compiler.Function.Chunk;

        static byte MakeConstant(State state, Value value)
        {
            var constantIndex = CurrentChunk(state).AddConstant(value);
            if (constantIndex > byte.MaxValue)
                Error(state, "Too many constants in one chunk");
            return (byte)constantIndex;
        }

        static int CurrentCodePosition(State state) => CurrentChunk(state).Code.Count;

        static void Emit(State state, byte value) => CurrentChunk(state).Write(value, state.Previous.Line);

        static void Emit(State state, OpCode value) => Emit(state, (byte)value);

        static void Emit(State state, OpCode op, byte arg)
        {
            Emit(state, op);
            Emit(state, arg);
        }

        static void EmitReturn(State state) => Emit(state, OpCode.Return);

        static void EmitConstant(State state, Value value) => Emit(state, OpCode.Constant, MakeConstant(state, value));

        static int EmitJump(State state, OpCode value)
        {
            Emit(state, value);
            Emit(state, 0xff);
            Emit(state, 0xff);
            return CurrentCodePosition(state) - 2;
        }

        static void EmitLoop(State state, int loopStart)
        {
            Emit(state, OpCode.Loop);

            var offset = CurrentChunk(state).Code.Count - loopStart + 2;
            if (offset > ushort.MaxValue)
                Error(state, "Loop body too large");

            Emit(state, (byte)((offset >> 8) & 0xff));
            Emit(state, (byte)(offset & 0xff));
        }

        static void PatchJump(State state, int offset)
        {
            var jump = CurrentCodePosition(state) - offset - 2;
            if (jump > ushort.MaxValue)
                Error(state, "Too much code to jump over");

            CurrentChunk(state).Code[offset] = (byte)((jump >> 8) & 0xff);
            CurrentChunk(state).Code[offset + 1] = (byte)(jump & 0xff);
        }

        static Function EndCompile(State state)
        {
            EmitReturn(state);

#if DEBUG
            if (!state.HadError)
            {
                foreach (var line in state.Compiler.Function.DisassembleFunction())
                    Console.WriteLine(line);
            }
#endif

            return state.Compiler.Function;
        }

        static void BeginScope(State state)
        {
            state.Compiler.ScopeDepth++;
        }

        static void EndScope(State state)
        {
            state.Compiler.ScopeDepth--;

            var originalLocalCount = state.Compiler.LocalCount;
            while (state.Compiler.LocalCount > 0 && state.Compiler.Locals[state.Compiler.LocalCount - 1].Depth > state.Compiler.ScopeDepth)
                state.Compiler.LocalCount--;

            var discardedLocals = originalLocalCount - state.Compiler.LocalCount;
            if (discardedLocals == 1)
                Emit(state, OpCode.Pop);
            else if (discardedLocals > 1)
                Emit(state, OpCode.PopN, (byte)discardedLocals);
        }

        #endregion

        #region Parsing Functions

        static void ParsePrecedence(State state, Precedence precedence)
        {
            Advance(state);
            var prefixRule = GetRule(state.Previous.Type).Prefix;
            if (prefixRule is null)
                Error(state, "Expect expression");
            else
            {
                var canAssign = precedence <= Precedence.Assignment;
                prefixRule(state, canAssign);

                while (precedence <= GetRule(state.Current.Type).Precedence)
                {
                    Advance(state);
                    var infixRule = GetRule(state.Previous.Type).Infix;
                    infixRule(state, canAssign);
                }

                if (canAssign && Match(state, TokenType.Equal))
                    Error(state, "Invalid assignment target");

            }
        }

        static int ResolveLocal(State state, Token name)
        {
            for (int i = state.Compiler.LocalCount - 1; i >= 0; i--)
            {
                if (state.Compiler.Locals[i].Token.Lexeme == name.Lexeme)
                {
                    if (state.Compiler.Locals[i].Depth == -1)
                        Error(state, "Cannot read variable in its own initializer");
                    return i;
                }
            }
            return -1;
        }

        static byte IdentifierConstant(State state, Token name)
        {
            return MakeConstant(state, Values.String(name.Lexeme));
        }

        static void DeclareVariable(State state)
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
                    Error(state, "Variable with this name already defined in this scope");
            }

            state.Compiler.AddLocal(state, name);
        }

        static byte ParseVariable(State state, string message)
        {
            Consume(state, TokenType.Identifier, message);

            DeclareVariable(state);
            if (state.Compiler.ScopeDepth > 0)
                return 0;

            return IdentifierConstant(state, state.Previous);
        }

        static void MarkInitialized(State state) => state.Compiler.Locals[state.Compiler.LocalCount - 1].Depth = state.Compiler.ScopeDepth;

        static void DefineVariable(State state, byte global)
        {
            if (state.Compiler.ScopeDepth > 0)
            {
                MarkInitialized(state);
                return;
            }

            Emit(state, OpCode.DefineGlobal, global);
        }

        static void And(State state, bool canAssign)
        {
            var endJump = EmitJump(state, OpCode.JumpIfFalse);

            Emit(state, OpCode.Pop);
            ParsePrecedence(state, Precedence.And);

            PatchJump(state, endJump);
        }

        static void Or(State state, bool canAssign)
        {
            var elseJump = EmitJump(state, OpCode.JumpIfFalse);
            var endJump = EmitJump(state, OpCode.Jump);

            PatchJump(state, elseJump);
            Emit(state, OpCode.Pop);

            ParsePrecedence(state, Precedence.Or);
            PatchJump(state, endJump);
        }

        static void Number(State state, bool canAssign)
        {
            var value = decimal.Parse(state.Previous.Lexeme, CultureInfo.InvariantCulture);
            EmitConstant(state, Values.Number(value));
        }

        static void Unary(State state, bool canAssign)
        {
            var operatorType = state.Previous.Type;

            ParsePrecedence(state, Precedence.Unary);

            switch(operatorType)
            {
                case TokenType.Bang:
                    Emit(state, OpCode.Not);
                    break;
                case TokenType.Minus:
                    Emit(state, OpCode.Negate);
                    break;

                default:
                    return;
            }
        }

        static void Binary(State state, bool canAssign)
        {
            var operatorType = state.Previous.Type;

            var rule = GetRule(operatorType);
            ParsePrecedence(state, rule.Precedence + 1);

            switch(operatorType)
            {
                case TokenType.Minus:
                    Emit(state, OpCode.Subtract);
                    break;
                case TokenType.Plus:
                    Emit(state, OpCode.Add);
                    break;
                case TokenType.Slash:
                    Emit(state, OpCode.Divide);
                    break;
                case TokenType.BangEqual:
                    Emit(state, OpCode.Equal);
                    Emit(state, OpCode.Not);
                    break;
                case TokenType.EqualEqual:
                    Emit(state, OpCode.Equal);
                    break;
                case TokenType.LessEqual:
                    Emit(state, OpCode.Greater);
                    Emit(state, OpCode.Not);
                    break;
                case TokenType.Greater:
                    Emit(state, OpCode.Greater);
                    break;
                case TokenType.GreaterEqual:
                    Emit(state, OpCode.Less);
                    Emit(state, OpCode.Not);
                    break;
                case TokenType.Less:
                    Emit(state, OpCode.Less);
                    break;
                case TokenType.Star:
                    Emit(state, OpCode.Multiply);
                    break;
                case TokenType.StarStar:
                    Emit(state, OpCode.Exp);
                    break;
                case TokenType.Mod:
                    Emit(state, OpCode.Modulo);
                    break;

                default:
                    return;
            }
        }

        static void Literal(State state, bool canAssign)
        {
            switch (state.Previous.Type)
            {
                case TokenType.True:
                    Emit(state, OpCode.True);
                    break;
                case TokenType.False:
                    Emit(state, (byte)OpCode.False);
                    break;
            }
        }

        static void NamedVariable(State state, Token name, bool canAssign)
        {
            OpCode get, set;
            var arg = ResolveLocal(state, name);
            if (arg != -1)
            {
                get = OpCode.LoadLocal;
                set = OpCode.AssignLocal;
            }
            else
            {
                arg = IdentifierConstant(state, name);
                get = OpCode.LoadGlobal;
                set = OpCode.AssignGlobal;
            }

            if (canAssign)
            {
                void EmitArithmeticAssignmentOperator(OpCode operation)
                {
                    Emit(state, get, (byte)arg);
                    Expression(state);
                    Emit(state, operation);
                    Emit(state, set, (byte)arg);
                }

                if (Match(state, TokenType.Equal))
                {
                    Expression(state);
                    Emit(state, set, (byte)arg);
                }
                else if (Match(state, TokenType.MinusEqual))
                    EmitArithmeticAssignmentOperator(OpCode.Subtract);
                else if (Match(state, TokenType.PlusEqual))
                    EmitArithmeticAssignmentOperator(OpCode.Add);
                else if (Match(state, TokenType.SlashEqual))
                    EmitArithmeticAssignmentOperator(OpCode.Divide);
                else if (Match(state, TokenType.StarEqual))
                    EmitArithmeticAssignmentOperator(OpCode.Multiply);
                else if (Match(state, TokenType.StarStarEqual))
                    EmitArithmeticAssignmentOperator(OpCode.Exp);
                else
                    Emit(state, get, (byte)arg);
            }
            else
                Emit(state, get, (byte)arg);
        }

        static void Variable(State state, bool canAssign)
        {
            NamedVariable(state, state.Previous, canAssign);
        }

        static void Grouping(State state, bool canAssign)
        {
            Expression(state);
            Consume(state, TokenType.ParenRight, "Expect ')' after expression");
        }

        static void Expression(State state)
        {
            ParsePrecedence(state, Precedence.Assignment);
        }

        static void Block(State state)
        {
            while (!Check(state, TokenType.BraceRight) && !Check(state, TokenType.EOF))
                Declaration(state);
            Consume(state, TokenType.BraceRight, "Expect '}' after block");
        }

        static void ExpressionStatement(State state)
        {
            Expression(state);
            Consume(state, TokenType.Semicolon, "Expect ';' after expression");
            Emit(state, OpCode.Pop);
        }

        static void ForStatement(State state)
        {
            BeginScope(state);

            // Initializer
            if (Match(state, TokenType.Var))
                VarDeclaration(state);
            else if (Match(state, TokenType.Semicolon))
            {
                // No initializer
            }   
            else
                ExpressionStatement(state);

            var surroundingLoopState = state.InnermostLoop;
            state.InnermostLoop = LoopState.FromCurrentState(state);

            // Condition
            var exitJump = -1;
            if (!Match(state, TokenType.Semicolon))
            {
                Expression(state);
                Consume(state, TokenType.Semicolon, "Expect ';' after loop condition");
                // Jump out if condition is negative
                exitJump = EmitJump(state, OpCode.JumpIfFalse);
                Emit(state, OpCode.Pop); // Condition
            }

            // Increment
            if (!Match(state, TokenType.Colon))
            {
                var bodyJump = EmitJump(state, OpCode.Jump);

                var incrementStart = CurrentCodePosition(state);
                Expression(state);
                Emit(state, OpCode.Pop);
                Consume(state, TokenType.Colon, "Expect ':' after for clauses");

                EmitLoop(state, state.InnermostLoop.Start);
                state.InnermostLoop.Start = incrementStart;
                PatchJump(state, bodyJump);
            }

            Statement(state);

            EmitLoop(state, state.InnermostLoop.Start);

            if (exitJump != -1)
            {
                PatchJump(state, exitJump);
                Emit(state, OpCode.Pop); // Condition, if Pop has been jumped over above
            }

            state.InnermostLoop.PatchUnresolvedBreaks(state);
            state.InnermostLoop = surroundingLoopState;

            EndScope(state);
        }

        static void IfStatement(State state)
        {
            Expression(state);
            Consume(state, TokenType.Colon, "Expect ':' after condition");

            var thenJump = EmitJump(state, OpCode.JumpIfFalse);
            Emit(state, OpCode.Pop);
            Statement(state);
            var elseJump = EmitJump(state, OpCode.Jump);

            PatchJump(state, thenJump);
            Emit(state, OpCode.Pop);

            if (Match(state, TokenType.Else))
            {
                Consume(state, TokenType.Colon, "Expect ':' after else");
                Statement(state);
            }

            PatchJump(state, elseJump);
        }

        static void PrintStatement(State state)
        {
            Expression(state);
            Consume(state, TokenType.Semicolon, "Expect ';' after value");
            Emit(state, OpCode.Print);
        }

        static void WhileStatement(State state)
        {
            var surroundingLoopState = state.InnermostLoop;
            state.InnermostLoop = LoopState.FromCurrentState(state);

            Expression(state);
            Consume(state, TokenType.Colon, "Expect ':' after condition");

            var exitJump = EmitJump(state, OpCode.JumpIfFalse);

            Emit(state, OpCode.Pop);
            Statement(state);

            EmitLoop(state, state.InnermostLoop.Start);

            PatchJump(state, exitJump);
            Emit(state, OpCode.Pop);

            state.InnermostLoop.PatchUnresolvedBreaks(state);
            state.InnermostLoop = surroundingLoopState;
        }

        static void ContinueStatement(State state)
        {
            if (state.InnermostLoop.Start == -1)
                Error(state, "Can't use continue outside of a loop");

            Consume(state, TokenType.Semicolon, "Expect ';' after continue");

            // Discard locals
            var popCount = 0;
            for (var i = state.Compiler.LocalCount - 1; i >= 0 && state.Compiler.Locals[i].Depth > state.InnermostLoop.ScopeDepth; i--)
                popCount++;
            if (popCount > 0)
                Emit(state, OpCode.PopN, (byte)popCount);

            EmitLoop(state, state.InnermostLoop.Start);
        }

        static void BreakStatement(State state)
        {
            if (state.InnermostLoop.Start == -1)
                Error(state, "Can't use break outside of a loop");

            Consume(state, TokenType.Semicolon, "Expect ';' after break");

            // Discard locals
            var popCount = 0;
            for (var i = state.Compiler.LocalCount - 1; i >= 0 && state.Compiler.Locals[i].Depth > state.InnermostLoop.ScopeDepth; i--)
                popCount++;
            if (popCount > 0)
                Emit(state, OpCode.PopN, (byte)popCount);

            var breakJump = EmitJump(state, OpCode.Jump);
            state.InnermostLoop.UnresolvedBreaks.Write(breakJump);
        }

        static void Statement(State state)
        {
            if (Match(state, TokenType.Print))
                PrintStatement(state);
            else if (Match(state, TokenType.For))
                ForStatement(state);
            else if (Match(state, TokenType.If))
                IfStatement(state);
            else if (Match(state, TokenType.While))
                WhileStatement(state);
            else if (Match(state, TokenType.Continue))
                ContinueStatement(state);
            else if (Match(state, TokenType.Break))
                BreakStatement(state);
            else if (Match(state, TokenType.BraceLeft))
            {
                BeginScope(state);
                Block(state);
                EndScope(state);
            }
            else
                ExpressionStatement(state);
        }

        static void VarDeclaration(State state)
        {
            var global = ParseVariable(state, "Expect variable name");

            Consume(state, TokenType.Equal, "Expect '=' after variable name");
            Expression(state);
            Consume(state, TokenType.Semicolon, "Expect ';' after variable declaration");
            DefineVariable(state, global);
        }

        static void Declaration(State state)
        {
            if (Match(state, TokenType.Var))
                VarDeclaration(state);
            else
                Statement(state);

            if (state.PanicMode)
                Synchronize(state);
        }

        #endregion

        public static Function Compile(string source)
        {
            var state = new State
            {
                Scanner = new Scanner(source ?? throw new ArgumentNullException(nameof(source))),
                Compiler = new Compiler(FunctionType.Script),
            };

            Advance(state);

            while (!Match(state, TokenType.EOF))
                Declaration(state);

            var function = EndCompile(state);
            return state.HadError ? null : function;
        }
    }
}
