using System;

namespace SpeedCalc.Core.Runtime
{
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
}
