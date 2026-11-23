namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies how a <see cref="Utf8XmlReader"/> handles whitespace-only text between elements.
    /// </summary>
    /// <remarks>
    /// Whitespace inside a leaf element's value is always significant.
    /// <c>&lt;n&gt; a &lt;/n&gt;</c> is the value <c>" a "</c> regardless of this setting.
    /// <see cref="Significant"/> is the zero value of the enumeration, so it is the default wherever a
    /// whitespace handling value is not set: in <see cref="XmlReaderOptions"/> and
    /// <see cref="XmlDocumentOptions"/>, whether they are created with <see langword="new"/> or as
    /// <see langword="default"/>. Whitespace between elements, such as indentation, is kept by default so
    /// that XML that is read and written back does not change its layout.
    /// <see cref="XmlSerializerOptions.WhitespaceHandling"/> defaults to <see cref="Insignificant"/>, because
    /// indentation has no place in an object.
    /// </remarks>
    public enum XmlWhitespaceHandling : byte
    {

        /// <summary>
        /// Whitespace-only text nodes are reported as <see cref="XmlTokenType.Text"/> tokens. This is the default.
        /// </summary>
        Significant = 0,

        /// <summary>
        /// Whitespace-only text between elements is skipped.
        /// </summary>
        Insignificant = 1,

        /// <summary>
        /// Whitespace-only text between elements is not allowed. The reader throws an
        /// <see cref="XmlException"/> when it reads such text.
        /// </summary>
        None = 2,

    }

}
