using System;

namespace SpeedCalc.Core.Runtime
{

    [Serializable]
    public class RuntimeException : Exception
    {
        public string VMStackTrace { get; }

        public RuntimeException() { }

        public RuntimeException(string message) : base(message) { }

        public RuntimeException(string message, Exception inner) : base(message, inner) { }

        public RuntimeException(string message, string stackTrace) : this(message) => VMStackTrace = stackTrace;

        public RuntimeException(string message, string stackTrace, Exception inner) : this(message, inner) => VMStackTrace = stackTrace;

        protected RuntimeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
