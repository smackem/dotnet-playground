using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileMachine.Lexing
{
    /// <summary>
    /// Simple character reader that allows "unreading" (buffering) chars.
    /// </summary>
    /// <remarks>
    /// This class is not thread-safe.
    /// </remarks>
    public class CharReader : IDisposable
    {
        readonly TextReader _reader;
        readonly Stack<int> _buffer = new Stack<int>();

        CharReader(TextReader reader) => _reader = reader;

        /// <summary>
        /// Creates a <see cref="CharReader"/> from the given <see cref="TextReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader"/> to read chars from.</param>
        /// <returns>A new instance of <see cref="CharReader"/>.</returns>
        public static CharReader FromTextReader(TextReader reader) =>
            new CharReader(reader ?? throw new ArgumentNullException(nameof(reader)));

        /// <summary>
        /// Reads and consumes the next char.
        /// </summary>
        /// <returns>The read char.</returns>
        public int Read() => _buffer.Count > 0
            ? _buffer.Pop()
            : _reader.Read();

        /// <summary>
        /// Buffers the given char so that the next call to <see cref="Read"/> will return this char.
        /// Buffered chars are read in LIFO order.
        /// </summary>
        /// <param name="ch">The char to buffer.</param>
        public void Unread(int ch) => _buffer.Push(ch);

        /// <summary>
        /// Closes the underlying <see cref="TextReader"/>.
        /// </summary>
        public void Dispose() => _reader.Dispose();
    }
}
