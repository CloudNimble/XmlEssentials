using System;
using System.Buffers;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A <see cref="ReadOnlySequenceSegment{T}"/> used to build multi-segment UTF-8 payloads.
    /// </summary>
    public sealed class ByteSequenceSegment : ReadOnlySequenceSegment<byte>
    {

        #region Constructors

        /// <summary>
        /// Creates a segment over <paramref name="memory"/> starting at <paramref name="runningIndex"/>.
        /// </summary>
        /// <param name="memory">The UTF-8 bytes in this segment.</param>
        /// <param name="runningIndex">The running index of this segment in the sequence.</param>
        public ByteSequenceSegment(ReadOnlyMemory<byte> memory, long runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Appends <paramref name="memory"/> as the next segment.
        /// </summary>
        /// <param name="memory">The next bytes.</param>
        /// <returns>The appended segment.</returns>
        public ByteSequenceSegment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new ByteSequenceSegment(memory, RunningIndex + Memory.Length);
            Next = next;
            return next;
        }

        #endregion

    }

}
