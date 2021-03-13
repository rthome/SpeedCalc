using System;

namespace SpeedCalc.Core.Runtime
{

    [Serializable]
    public class RuntimeValueTypeException : RuntimeException
    {
        public RuntimeValueTypeException() { }

        public RuntimeValueTypeException(string message) : base(message) { }

        public RuntimeValueTypeException(string message, Exception inner) : base(message, inner) { }

        public RuntimeValueTypeException(string message, string stackTrace) : base(message, stackTrace) { }

        public RuntimeValueTypeException(string message, string stackTrace, Exception inner) : base(message, stackTrace, inner) { }

        protected RuntimeValueTypeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
