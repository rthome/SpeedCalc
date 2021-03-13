using System;

namespace SpeedCalc.Core.Runtime
{

    [Serializable]
    public class RuntimeExecutionException : RuntimeException
    {
        public RuntimeExecutionException() { }

        public RuntimeExecutionException(string message) : base(message) { }

        public RuntimeExecutionException(string message, Exception inner) : base(message, inner) { }

        public RuntimeExecutionException(string message, string stackTrace) : base(message, stackTrace) { }

        public RuntimeExecutionException(string message, string stackTrace, Exception inner) : base(message, stackTrace, inner) { }

        protected RuntimeExecutionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
