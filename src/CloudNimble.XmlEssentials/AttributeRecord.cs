namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// One attribute on the current start tag: name slices and an optional decoded value.
    /// </summary>
    /// <remarks>
    /// Name and value offsets are into the reader's current UTF-8 buffer.
    /// <see cref="Decoded"/> is set only when entities or attribute-value normalization
    /// produced a buffer distinct from the raw slice.
    /// </remarks>
    internal struct AttributeRecord
    {

        #region Fields

        /// <summary>
        /// Decoded attribute value when entities or whitespace normalization produced a new buffer.
        /// </summary>
        public byte[]? Decoded;

        /// <summary>
        /// Number of bytes of <see cref="Decoded"/> that belong to this attribute.
        /// </summary>
        public int DecodedLength;

        /// <summary>
        /// Offset of this attribute's value in <see cref="Decoded"/>, which is shared by the start tag's attributes.
        /// </summary>
        public int DecodedStart;

        /// <summary>
        /// UTF-8 byte length of the raw name, including a prefix and colon when present.
        /// </summary>
        public int NameLength;

        /// <summary>
        /// Byte offset of the name in the current buffer.
        /// </summary>
        public int NameStart;

        /// <summary>
        /// UTF-8 byte length of the prefix, or 0 when the attribute is unprefixed.
        /// </summary>
        public int PrefixLength;

        /// <summary>
        /// True when the value contained an entity or had whitespace normalized.
        /// </summary>
        public bool ValueIsEscaped;

        /// <summary>
        /// UTF-8 byte length of the raw value in the buffer.
        /// </summary>
        public int ValueLength;

        /// <summary>
        /// Byte offset of the raw value in the current buffer.
        /// </summary>
        public int ValueStart;

        #endregion

    }

}
