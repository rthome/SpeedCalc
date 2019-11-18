using System;

namespace SpeedCalc.Core.Runtime
{

    [Serializable]
    public class RuntimeExecutionException : RuntimeException
    {
        public RuntimeExecutionException() { }

        public RuntimeExecutionException(string message) : base(message) { }

        public RuntimeExecutionException(string message, Exception inner) : base(message, inner) { }

        protected RuntimeExecutionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
