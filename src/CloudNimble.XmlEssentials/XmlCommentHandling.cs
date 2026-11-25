namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies how a <see cref="Utf8XmlReader"/> handles comments.
    /// </summary>
    /// <remarks>
    /// <see cref="Allow"/> is the zero value of the enumeration, so it is the default wherever a comment
    /// handling value is not set: in <see cref="XmlReaderOptions"/> and <see cref="XmlDocumentOptions"/>,
    /// whether they are created with <see langword="new"/> or as <see langword="default"/>. Comments are kept
    /// by default so that XML that is read and written back does not lose them.
    /// <see cref="XmlSerializerOptions.ReadCommentHandling"/> defaults to <see cref="Skip"/>, because a
    /// comment has no place in an object.
    /// </remarks>
    public enum XmlCommentHandling : byte
    {

        /// <summary>
        /// Comments are reported as <see cref="XmlTokenType.Comment"/> tokens. This is the default.
        /// </summary>
        Allow = 0,

        /// <summary>
        /// Comments are consumed and not reported.
        /// </summary>
        Skip = 1,

        /// <summary>
        /// Comments are not allowed. The reader throws an <see cref="XmlException"/> when it reads one.
        /// </summary>
        Disallow = 2,

    }

}
