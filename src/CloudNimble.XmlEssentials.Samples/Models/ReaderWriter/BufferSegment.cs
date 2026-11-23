using System;
using System.Buffers;

namespace CloudNimble.XmlEssentials.Samples.Models.ReaderWriter
{

    /// <summary>
    /// Represents one block of a <see cref="ReadOnlySequence{T}"/> of bytes that is made of several blocks.
    /// </summary>
    public sealed class BufferSegment : ReadOnlySequenceSegment<byte>
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferSegment"/> class that holds the specified bytes.
        /// </summary>
        /// <param name="memory">The bytes of this block.</param>
        public BufferSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a block after this one.
        /// </summary>
        /// <param name="memory">The bytes of the next block.</param>
        /// <returns>The new block, which is now the last one.</returns>
        public BufferSegment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new BufferSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length,
            };

            Next = next;
            return next;
        }

        #endregion

    }

}
