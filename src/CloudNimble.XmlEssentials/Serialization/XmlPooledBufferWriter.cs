using System;
using System.Buffers;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// An <see cref="IBufferWriter{T}"/> over arrays rented from <see cref="ArrayPool{T}.Shared"/>.
    /// </summary>
    /// <remarks>
    /// The serializer writes a whole document here before copying it out as a string, an array, or a single
    /// asynchronous write. An instance holds no array between <see cref="Return"/> and the next
    /// <see cref="Rent"/>, so one can stay in a thread-static cache without pinning memory.
    /// </remarks>
    internal sealed class XmlPooledBufferWriter : IBufferWriter<byte>
    {

        #region Fields

        private byte[] _buffer = [];
        private int _written;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the bytes written so far.
        /// </summary>
        internal ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _written);

        /// <summary>
        /// Gets the bytes written so far.
        /// </summary>
        internal ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _written);

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public void Advance(int count)
        {
            _written += count;
        }

        /// <inheritdoc />
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            var needed = Math.Max(sizeHint, 1);
            if (_buffer.Length - _written < needed)
            {
                var grown = ArrayPool<byte>.Shared.Rent(Math.Max(_buffer.Length * 2, _written + needed));
                WrittenSpan.CopyTo(grown);
                XmlDocument.ReturnBuffer(_buffer, _written);
                _buffer = grown;
            }

            return _buffer.AsMemory(_written);
        }

        /// <inheritdoc />
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            return GetMemory(sizeHint).Span;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Rents the first array.
        /// </summary>
        /// <param name="capacity">The smallest acceptable array length.</param>
        internal void Rent(int capacity)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(capacity);
        }

        /// <summary>
        /// Clears what was written and gives the array back to the pool.
        /// </summary>
        internal void Return()
        {
            XmlDocument.ReturnBuffer(_buffer, _written);
            _buffer = [];
            _written = 0;
        }

        #endregion

    }

}
