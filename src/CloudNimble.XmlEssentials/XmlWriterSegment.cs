namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// The kind of value a <see cref="Utf8XmlWriter"/> is part way through writing in segments.
    /// </summary>
    internal enum XmlWriterSegment : byte
    {

        /// <summary>
        /// No segmented value is open.
        /// </summary>
        None = 0,

        /// <summary>
        /// Text given as UTF-8 bytes. The bytes carried over are the start of a character.
        /// </summary>
        Utf8Text = 1,

        /// <summary>
        /// Text given as UTF-16 characters. The character carried over is a high surrogate.
        /// </summary>
        CharText = 2,

        /// <summary>
        /// Binary data written as base64. The bytes carried over are the start of a group of three.
        /// </summary>
        Base64 = 3,

    }

}
