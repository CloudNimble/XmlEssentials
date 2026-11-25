namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// One attribute on the current start tag: name slices and an optional decoded value.
    /// </summary>
    /// <remarks>
    /// Name and value offsets are into the reader's current UTF-8 buffer.
    /// <see cref="DecodedStart"/> is zero or greater only when entities or attribute-value normalization
    /// produced bytes distinct from the raw slice. The record holds no object reference, so the reader's
    /// inline slots for it need not be cleared when a reader is constructed.
    /// </remarks>
    internal struct AttributeRecord
    {

        #region Fields

        /// <summary>
        /// <see cref="NsIndex"/> of the first in-scope binding; binding <c>n</c> is stored as <c>FirstBinding + n</c>.
        /// </summary>
        public const int FirstBinding = 3;

        /// <summary>
        /// <see cref="NsIndex"/> of an unprefixed attribute, which is in no namespace.
        /// </summary>
        public const int NoNamespace = 0;

        /// <summary>
        /// <see cref="NsIndex"/> of a prefixed attribute whose prefix has not been looked up yet.
        /// </summary>
        public const int UnresolvedNamespace = -1;

        /// <summary>
        /// <see cref="NsIndex"/> of an attribute with the <c>xml</c> prefix.
        /// </summary>
        public const int XmlNamespace = 2;

        /// <summary>
        /// <see cref="NsIndex"/> of an <c>xmlns</c> or <c>xmlns:prefix</c> declaration.
        /// </summary>
        public const int XmlnsNamespace = 1;

        /// <summary>
        /// Number of decoded bytes that belong to this attribute.
        /// </summary>
        public int DecodedLength;

        /// <summary>
        /// Offset of this attribute's decoded value in the buffer the start tag's attributes share, or -1 when the
        /// value needed no decoding and is read from the input.
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
        /// The attribute's namespace, resolved once when its start tag is completed: one of the constants
        /// on this type, or <see cref="FirstBinding"/> plus the index of the in-scope binding.
        /// </summary>
        public int NsIndex;

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
