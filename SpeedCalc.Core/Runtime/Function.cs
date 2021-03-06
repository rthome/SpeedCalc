using System;

namespace SpeedCalc.Core.Runtime
{
    public sealed class Function
    {
        public string Name { get; }

        public int Arity { get; }

        public Chunk Chunk { get; }

        public override string ToString() => string.IsNullOrEmpty(Name) ? "<script>" : $"<fn {Name} '{Arity}>";

        public Function(string name, int arity)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Arity = arity;
            Chunk = new Chunk();
        }
    }
}
