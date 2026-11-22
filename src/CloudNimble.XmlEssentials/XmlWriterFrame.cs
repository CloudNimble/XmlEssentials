namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// One in-progress start tag on the writer stack.
    /// </summary>
    internal struct XmlWriterFrame
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the number of attributes written on this start tag, including xmlns*.
        /// </summary>
        public int AttributeCount { get; set; }

        /// <summary>
        /// Gets or sets whether content has been written (so the end tag cannot be empty).
        /// </summary>
        public bool HasContent { get; set; }

        /// <summary>
        /// Gets or sets whether character data has been written, which turns off indentation inside the element.
        /// </summary>
        public bool HasText { get; set; }

        /// <summary>
        /// Gets or sets the local name.
        /// </summary>
        public XmlWriterName Local { get; set; }

        /// <summary>
        /// Gets or sets the original local name when the start tag was written from a <see cref="string"/>.
        /// </summary>
        /// <remarks>
        /// BCL <c>XmlWellFormedWriter.ElementScope</c> keeps interned string names and re-emits them on
        /// the end tag, so the writer does not copy UTF-8 into a name store for literals such as
        /// <c>item</c> or <c>catalog</c>.
        /// </remarks>
        public string? LocalChars { get; set; }

        /// <summary>
        /// Gets or sets the prefix, or empty when unprefixed.
        /// </summary>
        public XmlWriterName Prefix { get; set; }

        /// <summary>
        /// Gets or sets the original prefix when the start tag was written from a <see cref="string"/>.
        /// </summary>
        public string? PrefixChars { get; set; }

        /// <summary>
        /// Gets or sets whether the start tag is still open for attributes.
        /// </summary>
        public bool TagOpen { get; set; }

        #endregion

    }

}
