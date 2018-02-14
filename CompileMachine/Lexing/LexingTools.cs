using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileMachine.Lexing
{
    static class LexingTools
    {
        public static bool IsOperator(char ch)
        {
            return ch == '('
                || ch == ')'
                || ch == '='
                || ch == '+'
                || ch == '-'
                || ch == '/'
                || ch == '*';
        }

        public static bool IsOperator(string s)
        {
            return s == "("
                || s == ")"
                || s == "="
                || s == "=="
                || s == "+"
                || s == "+="
                || s == "-"
                || s == "-="
                || s == "/"
                || s == "/="
                || s == "*"
                || s == "*=";
        }

        public static bool IsValidChar(int ch)
        {
            var c = (char)ch;

            return ch < 0
                || Char.IsLetterOrDigit(c)
                || Char.IsWhiteSpace(c)
                || IsOperator(c)
                || c == '"';
        }
    }
}
