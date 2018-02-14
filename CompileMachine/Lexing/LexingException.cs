using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileMachine.Lexing
{
    [Serializable]
    public class LexingException : Exception
    {
        public LexingException() { }
        public LexingException(string message) : base(message) { }
        public LexingException(string message, Exception inner) : base(message, inner) { }
        protected LexingException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
