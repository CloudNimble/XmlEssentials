namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Token produced by <see cref="Utf8XmlReader"/> as it walks a UTF-8 XML document.
    /// </summary>
    /// <remarks>
    /// Empty elements (<c>&lt;e/&gt;</c>) are reported as <see cref="StartElement"/> with
    /// <see cref="Utf8XmlReader.IsEmptyElement"/> set. The reader does not synthesize an
    /// <see cref="EndElement"/> token. Callers must check <see cref="Utf8XmlReader.IsEmptyElement"/>
    /// before expecting a matching end tag.
    /// </remarks>
    public enum XmlTokenType : byte
    {

        /// <summary>
        /// No token has been read yet, or the reader has reached the end of a complete document.
        /// </summary>
        None = 0,

        /// <summary>
        /// A start tag, including empty elements such as <c>&lt;e/&gt;</c>.
        /// </summary>
        StartElement = 1,

        /// <summary>
        /// An end tag such as <c>&lt;/e&gt;</c>.
        /// </summary>
        EndElement = 2,

        /// <summary>
        /// An attribute selected by <see cref="Utf8XmlReader.MoveToNextAttribute"/>.
        /// </summary>
        Attribute = 3,

        /// <summary>
        /// Character data, after entity expansion and end-of-line handling.
        /// </summary>
        Text = 4,

        /// <summary>
        /// A CDATA section. The value is the payload between <c>&lt;![CDATA[</c> and <c>]]&gt;</c>.
        /// </summary>
        CData = 5,

        /// <summary>
        /// A comment, reported only when <see cref="XmlCommentHandling.Allow"/> is set.
        /// </summary>
        Comment = 6,

        /// <summary>
        /// A processing instruction other than the XML declaration.
        /// </summary>
        ProcessingInstruction = 7,

        /// <summary>
        /// The XML declaration (<c>&lt;?xml ...?&gt;</c>), which must be the first token in the document.
        /// </summary>
        XmlDeclaration = 8,

    }

}
