namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// How <see cref="Utf8XmlReader"/> treats comment tokens.
    /// </summary>
    /// <remarks>
    /// The default for <see cref="XmlReaderOptions"/> is <see cref="Skip"/>.
    /// <see cref="Disallow"/> is the enum zero-value, so construct options with
    /// <c>new XmlReaderOptions()</c> rather than <c>default</c> when you want the documented default.
    /// </remarks>
    public enum XmlCommentHandling : byte
    {

        /// <summary>
        /// Comments are a well-formedness error and throw <see cref="XmlException"/>.
        /// </summary>
        Disallow = 0,

        /// <summary>
        /// Comments are consumed and not reported. This is the <see cref="XmlReaderOptions"/> default.
        /// </summary>
        Skip = 1,

        /// <summary>
        /// Comments are reported as <see cref="XmlTokenType.Comment"/>.
        /// </summary>
        Allow = 2,

    }

}
