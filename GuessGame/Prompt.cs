using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;

namespace GuessGame
{
    public sealed class Prompt : CodeActivity<int>
    {
        public InArgument<string> Text { get; set; }

        protected override int Execute(CodeActivityContext context)
        {
            Console.Write("{0}: ", context.GetValue(Text));
            var input = Console.ReadLine();
            return int.Parse(input);
        }
    }
}
