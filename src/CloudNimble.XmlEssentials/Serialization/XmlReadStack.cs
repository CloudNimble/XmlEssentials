using System.Collections.Generic;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// The frames of a value that is being read from a stream, outermost first.
    /// </summary>
    /// <remarks>
    /// It is the counterpart of <c>ReadStack</c> in System.Text.Json. When a block of input ends in the middle
    /// of a value the frames stay as they are, and the read goes on from the innermost one when the next block
    /// arrives. The frame objects are kept and used again, so reading a long sequence allocates them once.
    /// </remarks>
    internal sealed class XmlReadStack
    {

        #region Fields

        private readonly List<XmlReadFrame> _frames = [];

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number of frames in use. It is zero between values.
        /// </summary>
        internal int Count { get; private set; }

        /// <summary>
        /// Gets the innermost frame.
        /// </summary>
        internal XmlReadFrame Top => _frames[Count - 1];

        /// <summary>
        /// Gets the frame at <paramref name="index"/>, counted from the outermost.
        /// </summary>
        /// <param name="index">The zero-based position.</param>
        internal XmlReadFrame this[int index] => _frames[index];

        #endregion

        #region Private Methods

        /// <summary>
        /// Drops every frame, after a value could not be read.
        /// </summary>
        internal void Clear()
        {
            Count = 0;
        }

        /// <summary>
        /// Drops the innermost frame.
        /// </summary>
        internal void Pop()
        {
            Count--;
        }

        /// <summary>
        /// Adds a frame inside the innermost one, at its start.
        /// </summary>
        /// <returns>The frame, with nothing pending.</returns>
        internal XmlReadFrame Push()
        {
            if (Count == _frames.Count)
            {
                _frames.Add(new XmlReadFrame());
            }

            var frame = _frames[Count++];
            frame.Binding = null;
            frame.ElementBuilder = null;
            frame.ElementCollection = null;
            frame.ElementIsNil = false;
            frame.ElementProperty = null;
            frame.LeafLength = 0;
            frame.Next = 0;
            frame.Pending = XmlReadPending.None;
            frame.Stage = XmlReadStage.Start;
            frame.State = default;
            frame.Type = null;
            frame.Wrapper = null;
            frame.WrapperBuilder = null;
            return frame;
        }

        #endregion

    }

}
