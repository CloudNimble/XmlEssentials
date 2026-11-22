namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// How <see cref="Utf8XmlReader"/> treats whitespace-only text between elements.
    /// </summary>
    /// <remarks>
    /// Whitespace inside a leaf element's value is always significant.
    /// <c>&lt;n&gt; a &lt;/n&gt;</c> is the value <c>" a "</c> regardless of this setting.
    /// </remarks>
    public enum XmlWhitespaceHandling : byte
    {
        /// <summary>
        /// Skip whitespace-only text between elements. This is the default.
        /// </summary>
        Insignificant = 0,

        /// <summary>
        /// Report whitespace-only text nodes as <see cref="XmlTokenType.Text"/>.
        /// </summary>
        Significant = 1,

        /// <summary>
        /// Reject whitespace-only text between elements with <see cref="XmlException"/>.
        /// </summary>
        None = 2,
    }

}
