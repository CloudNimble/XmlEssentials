namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// One node in the immutable <see cref="XmlDocument"/> row store.
    /// </summary>
    /// <remarks>
    /// Rows are appended in document order: an element row, then its attribute rows, then its
    /// descendants. Attributes, children, and siblings are therefore derived from row positions
    /// instead of stored links, which keeps a row at 32 bytes (STJ <c>JsonDocument.DbRow</c> shape).
    /// Offsets refer to the document's UTF-8 address space; see <see cref="XmlDocument.Slice"/>.
    /// </remarks>
    internal struct XmlDbRow
    {

        #region Fields

        /// <summary>
        /// The value offset for attributes and leaf nodes, or the end row index for elements.
        /// </summary>
        private int _valueStartOrEndIndex;

        /// <summary>
        /// The value length for attributes and leaf nodes, or the attribute count for elements.
        /// </summary>
        private int _valueLengthOrAttributeCount;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the number of attribute rows that directly follow this element row.
        /// Valid for element rows only.
        /// </summary>
        public int AttributeCount
        {
            readonly get => _valueLengthOrAttributeCount;
            set => _valueLengthOrAttributeCount = value;
        }

        /// <summary>
        /// Gets or sets the index of the first row after this element's subtree.
        /// Valid for element rows only.
        /// </summary>
        public int EndIndex
        {
            readonly get => _valueStartOrEndIndex;
            set => _valueStartOrEndIndex = value;
        }

        /// <summary>
        /// Gets or sets whether this element was written as an empty-element tag (<c>&lt;e/&gt;</c>).
        /// </summary>
        public bool IsEmpty { get; set; }

        /// <summary>
        /// Gets or sets the node kind.
        /// </summary>
        public XmlValueKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the qualified-name byte length, including the prefix and colon when present.
        /// </summary>
        public int NameLength { get; set; }

        /// <summary>
        /// Gets or sets the qualified-name byte offset.
        /// </summary>
        public int NameStart { get; set; }

        /// <summary>
        /// Gets or sets the namespace URI byte length.
        /// </summary>
        public int NsLength { get; set; }

        /// <summary>
        /// Gets or sets the namespace URI byte offset.
        /// </summary>
        public int NsStart { get; set; }

        /// <summary>
        /// Gets or sets the prefix byte length, or 0 when the name is unprefixed.
        /// </summary>
        public int PrefixLength { get; set; }

        /// <summary>
        /// Gets or sets the value byte length. Valid for attribute and leaf rows only.
        /// </summary>
        public int ValueLength
        {
            readonly get => _valueLengthOrAttributeCount;
            set => _valueLengthOrAttributeCount = value;
        }

        /// <summary>
        /// Gets or sets the value byte offset. Valid for attribute and leaf rows only.
        /// </summary>
        public int ValueStart
        {
            readonly get => _valueStartOrEndIndex;
            set => _valueStartOrEndIndex = value;
        }

        #endregion

    }

}
