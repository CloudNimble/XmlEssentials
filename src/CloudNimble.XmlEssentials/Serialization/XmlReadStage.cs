namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// How far a frame has come in reading its element.
    /// </summary>
    internal enum XmlReadStage : byte
    {

        /// <summary>
        /// The reader is on the start tag, and nothing of the element has been read.
        /// </summary>
        Start = 0,

        /// <summary>
        /// The start tag has been read, and the child elements are being read.
        /// </summary>
        Content = 1,

        /// <summary>
        /// The end tag has been read, and the value is ready to be finished.
        /// </summary>
        End = 2,

    }

}
