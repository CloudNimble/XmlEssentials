using System;
using System.Buffers;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A buffer writer that never has any room, as a destination that is full would behave.
    /// </summary>
    internal sealed class EmptyBufferWriter : IBufferWriter<byte>
    {

        #region Public Methods

        /// <inheritdoc />
        public void Advance(int count)
        {
        }

        /// <inheritdoc />
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            return Memory<byte>.Empty;
        }

        /// <inheritdoc />
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            return [];
        }

        #endregion

    }

}
