using System;
using System.Buffers;
using System.IO;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A buffer writer that hands out exactly the number of bytes it is asked for, in a new array each time.
    /// </summary>
    /// <remarks>
    /// A writer that formats a value into the room it reserved, and reserved too little, is not caught by a
    /// destination that always has room to spare. With this one the room that was asked for is all there is, so
    /// a value that does not fit is lost or throws.
    /// </remarks>
    internal sealed class ExactBufferWriter : IBufferWriter<byte>
    {

        #region Fields

        private byte[] _current = [];
        private readonly MemoryStream _written = new();

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public void Advance(int count)
        {
            _written.Write(_current, 0, count);
            _current = [];
        }

        /// <inheritdoc />
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            _current = new byte[Math.Max(sizeHint, 1)];
            return _current;
        }

        /// <inheritdoc />
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            return GetMemory(sizeHint).Span;
        }

        /// <summary>
        /// Returns everything that has been advanced over.
        /// </summary>
        /// <returns>The bytes written so far.</returns>
        public byte[] ToArray()
        {
            return _written.ToArray();
        }

        #endregion

    }

}
