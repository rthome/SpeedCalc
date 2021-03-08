namespace SpeedCalc.Core.Runtime
{
    public enum FunctionType
    {
        Function,
        Script,
    }

    public struct Local
    {
        public Token Token;
        public int Depth;
    }

    public sealed class Compiler
    {
        public Function Function { get; set; }

        public FunctionType FunctionType { get; set; }

        public Local[] Locals { get; } = new Local[byte.MaxValue + 1];

        public int LocalCount { get; set; }

        public int ScopeDepth { get; set; }

        public void AddLocal(State state, Token name)
        {
            if (LocalCount == byte.MaxValue)
                state.Error("Too many locals in function");

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
}
