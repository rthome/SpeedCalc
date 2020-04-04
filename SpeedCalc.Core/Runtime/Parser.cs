using System;
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
        public sealed class State
        {
            public Scanner Scanner { get; set; }

            public Chunk CompilingChunk { get; set; }

            public Token Current { get; set; }

            public Token Previous { get; set; }

            public bool HadError { get; set; }

            public bool PanicMode { get; set; }
        }

        delegate void ParseFn(State state);

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
            new Rule(null,     null,   Precedence.None),       // Semicolon
            new Rule(Unary,    Binary, Precedence.Term),       // Minus
            new Rule(null,     Binary, Precedence.Term),       // Plus
            new Rule(null,     Binary, Precedence.Factor),     // Slash
            new Rule(Unary,    null,   Precedence.None),       // Bang
            new Rule(null,     Binary, Precedence.Equality),   // BangEqual
            new Rule(null,     null,   Precedence.None),       // Equal
            new Rule(null,     Binary, Precedence.Equality),   // EqualEqual
            new Rule(null,     Binary, Precedence.Comparison), // Greater
            new Rule(null,     Binary, Precedence.Comparison), // GreaterEqual
            new Rule(null,     Binary, Precedence.Comparison), // Less
            new Rule(null,     Binary, Precedence.Comparison), // LessEqual
            new Rule(null,     Binary, Precedence.Factor),     // Star
            new Rule(null,     Binary, Precedence.Exponent),   // StarStar
            new Rule(null,     null,   Precedence.None),       // Identifier
            new Rule(Number,   null,   Precedence.None),       // Number
            new Rule(null,     null,   Precedence.None),       // And
            new Rule(null,     null,   Precedence.None),       // Else
            new Rule(Literal,  null,   Precedence.None),       // False
            new Rule(null,     null,   Precedence.None),       // Fn,
            new Rule(null,     null,   Precedence.None),       // For
            new Rule(null,     null,   Precedence.None),       // If
            new Rule(null,     null,   Precedence.None),       // Or
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

        static byte MakeConstant(State state, Value value)
        {
            var constantIndex = state.CompilingChunk.AddConstant(value);
            if (constantIndex > byte.MaxValue)
                Error(state, "Too many constants in one chunk");
            return (byte)constantIndex;
        }

        static void Emit(State state, byte value) => state.CompilingChunk.Write(value, state.Previous.Line);

        static void Emit(State state, OpCode value) => Emit(state, (byte)value);

        static void Emit(State state, OpCode op, byte arg)
        {
            Emit(state, op);
            Emit(state, arg);
        }

        static void EmitReturn(State state) => Emit(state, OpCode.Return);

        static void EmitConstant(State state, Value value) => Emit(state, OpCode.Constant, MakeConstant(state, value));

        static void EndCompile(State state)
        {
            EmitReturn(state);

            if (!state.HadError)
            {
                foreach (var instruction in state.CompilingChunk.DisassembleChunk())
                    Console.WriteLine(instruction);
            }
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
                prefixRule(state);

                while (precedence <= GetRule(state.Current.Type).Precedence)
                {
                    Advance(state);
                    var infixRule = GetRule(state.Previous.Type).Infix;
                    infixRule?.Invoke(state);
                }
            }
        }

        static void Number(State state)
        {
            var value = decimal.Parse(state.Previous.Lexeme, CultureInfo.InvariantCulture);
            EmitConstant(state, Values.Number(value));
        }

        static void Unary(State state)
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

        static void Binary(State state)
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

                default:
                    return;
            }
        }

        static void Literal(State state)
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

        static void Grouping(State state)
        {
            Expression(state);
            Consume(state, TokenType.ParenRight, "Expect ')' after expression");
        }

        static void Expression(State state)
        {
            ParsePrecedence(state, Precedence.Assignment);
        }

        static void ExpressionStatement(State state)
        {
            Expression(state);
            Consume(state, TokenType.Semicolon, "Expect ';' after expression");
            Emit(state, OpCode.Pop);
        }

        static void PrintStatement(State state)
        {
            Expression(state);
            Consume(state, TokenType.Semicolon, "Expect ';' after value");
            Emit(state, OpCode.Print);
        }

        static void Statement(State state)
        {
            if (Match(state, TokenType.Print))
                PrintStatement(state);
            else
                ExpressionStatement(state);
        }

        static void Declaration(State state)
        {
            Statement(state);

            if (state.PanicMode)
                Synchronize(state);
        }

        #endregion

        public static bool Compile(string source, Chunk chunk)
        {
            var state = new State
            {
                Scanner = new Scanner(source ?? throw new ArgumentNullException(nameof(source))),
                CompilingChunk = chunk ?? throw new ArgumentNullException(nameof(chunk)),
            };

            Advance(state);

            while (!Match(state, TokenType.EOF))
                Declaration(state);

            Consume(state, TokenType.EOF, "Expect end of expression");
            EndCompile(state);

            return !state.HadError;
        }
    }
}
