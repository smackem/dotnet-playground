using System;
using System.Linq;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;

namespace CompileMachine
{
    class Program
    {
        static void Main(string[] args)
        {
            var lexer = new Lexing.Lexer();
            var arguments = new Dictionary<string, object>
            {
                { nameof(Lexing.Lexer.Reader), Lexing.CharReader.FromTextReader(Console.In) }
            };

            var result = WorkflowInvoker.Invoke(lexer, arguments);

            if (result[nameof(Lexing.Lexer.Lexemes)] is IEnumerable<string> lexemes)
            {
                foreach (var lexeme in lexemes)
                    Console.WriteLine("{0}", lexeme);
            }
            else
            {
                Console.WriteLine("Error");
            }

            Console.Write("Finished...");
        }
    }
}
