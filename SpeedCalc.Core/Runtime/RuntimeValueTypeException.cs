using System;

namespace SpeedCalc.Core.Runtime
{

    [Serializable]
    public class RuntimeValueTypeException : RuntimeException
    {
        public RuntimeValueTypeException() { }
        
        public RuntimeValueTypeException(string message) : base(message) { }
        
        public RuntimeValueTypeException(string message, Exception inner) : base(message, inner) { }

        protected RuntimeValueTypeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
