namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Options for <see cref="XmlDocument.Parse(string, XmlDocumentOptions)"/>.
    /// </summary>
    /// <remarks>
    /// A max-* of 0 means the default from the DoS table, except <see cref="MaxDocumentByteCount"/>
    /// which stays unbounded. Construct with <c>new XmlDocumentOptions()</c> so
    /// <see cref="CommentHandling"/> defaults to <see cref="XmlCommentHandling.Skip"/>.
    /// </remarks>
    public struct XmlDocumentOptions
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets how comments are handled.
        /// Default is <see cref="XmlCommentHandling.Skip"/> when constructed with <see cref="XmlDocumentOptions()"/>.
        /// </summary>
        public XmlCommentHandling CommentHandling { get; set; } = XmlCommentHandling.Skip;

        /// <summary>
        /// Gets or sets the maximum attributes on one element, including xmlns*.
        /// 0 means 1000.
        /// </summary>
        public int MaxAttributeCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum comment payload in UTF-8 bytes.
        /// 0 means 1,048,576.
        /// </summary>
        public int MaxCommentByteCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum element depth.
        /// 0 means 64.
        /// </summary>
        public int MaxDepth { get; set; }

        /// <summary>
        /// Gets or sets the maximum document size in bytes.
        /// 0 means unbounded.
        /// </summary>
        public int MaxDocumentByteCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum UTF-8 byte length of an element or attribute name.
        /// 0 means 1024.
        /// </summary>
        public int MaxNameByteCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum UTF-8 byte length of a text or CDATA node.
        /// 0 means 16,777,216.
        /// </summary>
        public int MaxTextByteCount { get; set; }

        /// <summary>
        /// Gets or sets how whitespace-only text between elements is handled.
        /// Default is <see cref="XmlWhitespaceHandling.Insignificant"/>.
        /// </summary>
        public XmlWhitespaceHandling WhitespaceHandling { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates options with <see cref="CommentHandling"/> set to <see cref="XmlCommentHandling.Skip"/>
        /// and every max-* at 0 (table defaults).
        /// </summary>
        public XmlDocumentOptions()
        {
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Copies these options onto a reader options struct.
        /// </summary>
        /// <returns>Reader options with the same limits and comment/whitespace handling.</returns>
        internal readonly XmlReaderOptions ToReaderOptions()
        {
            return new XmlReaderOptions
            {
                CommentHandling = CommentHandling,
                MaxAttributeCount = MaxAttributeCount,
                MaxCommentByteCount = MaxCommentByteCount,
                MaxDepth = MaxDepth,
                MaxDocumentByteCount = MaxDocumentByteCount,
                MaxNameByteCount = MaxNameByteCount,
                MaxTextByteCount = MaxTextByteCount,
                WhitespaceHandling = WhitespaceHandling,
            };
        }

        #endregion

    }

}
