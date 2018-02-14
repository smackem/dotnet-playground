using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileMachine.Lexing
{
    /// <summary>
    /// A simple lexer that splits source text into lexemes of these kinds:
    /// <list type="bullet">
    ///     <item>Words (e.g. <code>hello</code>, <code>wtf123</code>)</item>
    ///     <item>Integer Literals (e.g. <code>123</code>)</item>
    ///     <item>Operators (e.g. <code>+ - +=</code>)</item>
    ///     <item>String Literals (e.g. <code>"hello, world"</code>)</item>
    /// </list>
    /// Whitespace is ignored.
    /// </summary>
    public sealed class CharwiseLexer : IDisposable
    {
        readonly CharReader _reader;
        readonly StringBuilder _buffer = new StringBuilder();
        char _currentChar;

        /// <summary>
        /// Initializes a new instance of <see cref="CharwiseLexer"/>.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader"/> to read from.</param>
        public CharwiseLexer(TextReader reader)
        {
            _reader = CharReader.FromTextReader(reader ?? throw new ArgumentNullException(nameof(reader)));
        }

        /// <summary>
        /// Lexes the specified string and returns a list of lexemes.
        /// </summary>
        /// <param name="source">The string to lex.</param>
        /// <returns>A list of lexemes or <code>null</code> if an error occurred.</returns>
        public static IReadOnlyList<string> Lex(string source)
        {
            using (var lexer = new CharwiseLexer(new StringReader(source)))
            {
                try
                {
                    return lexer.Lex().ToList();
                }
                catch (LexingException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Lazily reads lexemes from the underlying reader.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> that yields the lexemes read from the underlying reader.</returns>
        /// <exception cref="LexingException">In case of an invalid character or an unclosed string literal.</exception>
        public IEnumerable<string> Lex()
        {
            while (Read())
            {
                switch (_currentChar)
                {
                    case char ch when Char.IsLetter(ch):
                        yield return ReadWord();
                        break;
                    case char ch when Char.IsDigit(ch):
                        yield return ReadNumber();
                        break;
                    case char ch when LexingTools.IsOperator(ch):
                        yield return ReadOperator();
                        break;
                    case '"':
                        yield return '"' + ReadString() + '"';
                        break;
                    case char ch when Char.IsWhiteSpace(ch):
                        break;
                    default:
                        throw new LexingException($"Invalid character '{_currentChar}'");
                }
            }
        }

        /// <summary>
        /// Closes the underlying <see cref="TextReader"/>.
        /// </summary>
        public void Dispose() => _reader.Dispose();

        bool Read()
        {
            int n = _reader.Read();

            if (n <= 0)
                return false;

            _currentChar = (char)n;
            return true;
        }

        string ReadWhile(Predicate<char> predicate)
        {
            var sb = _buffer.Clear();

            while (true)
            {
                if (predicate(_currentChar))
                {
                    sb.Append(_currentChar);
                }
                else
                {
                    _reader.Unread(_currentChar);
                    return sb.ToString();
                }

                if (Read() == false)
                    return sb.ToString();
            }
        }

        string ReadWord() => ReadWhile(Char.IsLetterOrDigit);

        string ReadNumber() => ReadWhile(Char.IsDigit);

        string ReadString()
        {
            var sb = _buffer.Clear();

            while (true)
            {
                if (Read() == false)
                    throw new LexingException("Unclosed String");

                if (_currentChar == '"')
                    return sb.ToString();

                sb.Append(_currentChar);
            }
        }

        string ReadOperator()
        {
            var str = "";

            while (true)
            {
                var testStr = str + _currentChar;

                if (LexingTools.IsOperator(testStr))
                {
                    str = testStr;
                }
                else
                {
                    _reader.Unread(_currentChar);
                    return str;
                }

                if (Read() == false)
                    return str;
            }
        }
    }
}
