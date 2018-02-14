using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;

namespace CompileMachine.Lexing
{

    public sealed class CharUnreaderActivity : CodeActivity
    {
        [RequiredArgument]
        public InArgument<int> Char { get; set; }

        [RequiredArgument]
        public InArgument<CharReader> Reader { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var ch = context.GetValue(Char);
            var reader = context.GetValue(Reader);

            reader.Unread(ch);
        }
    }
}
