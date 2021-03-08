﻿using System;

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
        public Compiler Enclosing { get; }

        public Function Function { get; }

        public FunctionType FunctionType { get; }

        public Local[] Locals { get; } = new Local[byte.MaxValue + 1];

        public int LocalCount { get; private set; }

        public int ScopeDepth { get; private set; }

        public void BeginScope()
        {
            ScopeDepth++;
        }

        public int EndScope()
        {
            ScopeDepth--;

            var originalLocalCount = LocalCount;
            while (LocalCount > 0 && Locals[LocalCount - 1].Depth > ScopeDepth)
                LocalCount--;

            // Number of discarded locals
            return originalLocalCount - LocalCount;
        }

        public void AddLocal(Action<string> errorFn, Token name)
        {
            if (LocalCount == byte.MaxValue)
                errorFn("Too many locals in function");

            var index = LocalCount++;
            Locals[index].Token = name;
            Locals[index].Depth = -1;
        }

        public Compiler(Compiler enclosing, FunctionType type, string functionName = "")
        {
            Enclosing = enclosing;
            FunctionType = type;
            Function = new Function(functionName, 0);

            var local = Locals[LocalCount++];
            local.Depth = 0;
            local.Token = new Token(TokenType.Error, string.Empty, 0);
        }
    }
}