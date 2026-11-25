namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// What a frame was in the middle of when the block of input ended.
    /// </summary>
    internal enum XmlReadPending : byte
    {

        /// <summary>
        /// Nothing: the frame is between the elements it reads.
        /// </summary>
        None = 0,

        /// <summary>
        /// The text of an element that holds one value.
        /// </summary>
        Leaf = 1,

        /// <summary>
        /// An element that is passed over.
        /// </summary>
        Skip = 2,

    }

}
