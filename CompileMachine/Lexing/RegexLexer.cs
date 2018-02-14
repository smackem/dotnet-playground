using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CompileMachine.Lexing
{
    public static class RegexLexer
    {
        static readonly Regex[] regexs = new[]
        {
            new Regex(@"^[A-Za-z]\w*", RegexOptions.Compiled),
            new Regex(@"^\d+", RegexOptions.Compiled),
            new Regex("^\"[^\"]*\"", RegexOptions.Compiled),
            new Regex(OperatorPattern("=="), RegexOptions.Compiled),
            new Regex(OperatorPattern("+="), RegexOptions.Compiled),
            new Regex(OperatorPattern("-="), RegexOptions.Compiled),
            new Regex(OperatorPattern("/="), RegexOptions.Compiled),
            new Regex(OperatorPattern("*="), RegexOptions.Compiled),
            new Regex(OperatorPattern("("), RegexOptions.Compiled),
            new Regex(OperatorPattern(")"), RegexOptions.Compiled),
            new Regex(OperatorPattern("="), RegexOptions.Compiled),
            new Regex(OperatorPattern("+"), RegexOptions.Compiled),
            new Regex(OperatorPattern("-"), RegexOptions.Compiled),
            new Regex(OperatorPattern("/"), RegexOptions.Compiled),
            new Regex(OperatorPattern("*"), RegexOptions.Compiled),
        };

        static string OperatorPattern(string op)
        {
            return "^" + Regex.Escape(op);
        }

        public static IReadOnlyList<string> Lex(string source)
        {
            var lexemes = new List<string>();

            for (int index = 0; index < source.Length; )
            {
                if (Char.IsWhiteSpace(source[index]))
                {
                    index++;
                    continue;
                }

                var m = FindMatch(source, index);

                if (m == null)
                    return null;

                lexemes.Add(m.Value);
                index += m.Length;
            }

            return lexemes;
        }

        static Match FindMatch(string source, int index)
        {
            source = source.Substring(index);

            return regexs
                .Select(regex => regex.Match(source))
                .FirstOrDefault(m => m.Success);
        }
    }
}
