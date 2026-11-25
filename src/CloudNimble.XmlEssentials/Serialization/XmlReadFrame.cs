using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// One level of a value that is being read from a stream: an object and its members, or a collection in a
    /// wrapper and its items. It holds everything the read needs to go on when more data arrives.
    /// </summary>
    /// <remarks>
    /// It is the counterpart of <c>ReadStackFrame</c> in System.Text.Json. A frame is kept by its
    /// <see cref="XmlReadStack"/> and used again for the next value at the same depth.
    /// </remarks>
    internal sealed class XmlReadFrame
    {

        #region Fields

        /// <summary>
        /// The binding of the object this frame reads, or null when it reads a collection in a wrapper.
        /// </summary>
        internal XmlObjectBinding? Binding;

        /// <summary>
        /// The collection that takes the value of the element being read, when that value is an item.
        /// </summary>
        internal XmlCollectionBinding? ElementCollection;

        /// <summary>
        /// The builder of <see cref="ElementCollection"/>.
        /// </summary>
        internal object? ElementBuilder;

        /// <summary>
        /// Whether the element being skipped was marked <c>xsi:nil</c>, so that null is stored when it has ended.
        /// </summary>
        internal bool ElementIsNil;

        /// <summary>
        /// The member that takes the value of the element being read, when that value is not an item.
        /// </summary>
        internal XmlBoundProperty? ElementProperty;

        /// <summary>
        /// The text gathered so far for an element whose content did not arrive in one piece.
        /// </summary>
        internal byte[] Leaf = [];

        /// <summary>
        /// The number of bytes of <see cref="Leaf"/> in use.
        /// </summary>
        internal int LeafLength;

        /// <summary>
        /// The member to try first for the next element, since a document usually has them in order.
        /// </summary>
        internal int Next;

        /// <summary>
        /// What is being read inside this frame that the block ended in the middle of.
        /// </summary>
        internal XmlReadPending Pending;

        /// <summary>
        /// Whether each member has appeared, one slot per member.
        /// </summary>
        internal bool[] Seen = [];

        /// <summary>
        /// The depth of the element being skipped, which is the depth of its end tag.
        /// </summary>
        internal int SkipDepth;

        /// <summary>
        /// How far the frame has come.
        /// </summary>
        internal XmlReadStage Stage;

        /// <summary>
        /// The object being read: its instance or held values, its collection builders, and the member whose
        /// element is being read.
        /// </summary>
        internal XmlObjectReadState State;

        /// <summary>
        /// The type of the object, for a message.
        /// </summary>
        internal Type? Type;

        /// <summary>
        /// The collection this frame fills, when it reads a collection in a wrapper.
        /// </summary>
        internal XmlCollectionBinding? Wrapper;

        /// <summary>
        /// The builder of <see cref="Wrapper"/>.
        /// </summary>
        internal object? WrapperBuilder;

        #endregion

    }

}
