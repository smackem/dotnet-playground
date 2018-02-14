using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;

namespace CompileMachine.Lexing
{
    public sealed class CharReaderActivity : CodeActivity<int>
    {
        [RequiredArgument]
        public InArgument<CharReader> Reader { get; set; }

        protected override int Execute(CodeActivityContext context)
        {
            var reader = context.GetValue(Reader);
            var n = reader.Read();

            if (n <= 0)
                return -1;

            return n;
        }
    }
}
