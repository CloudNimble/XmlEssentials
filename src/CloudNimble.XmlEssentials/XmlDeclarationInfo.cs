namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Represents the XML declaration captured when an <see cref="XmlDocument"/> is parsed.
    /// </summary>
    /// <remarks>
    /// The value is immutable. Mutable trees use <c>CloudNimble.Text.Xml.Nodes.XmlDeclarationNode</c> instead.
    /// </remarks>
    public readonly struct XmlDeclarationInfo
    {

        #region Public Properties

        /// <summary>
        /// Gets the encoding named in the declaration.
        /// </summary>
        /// <value>
        /// The name of the encoding. This is always <c>utf-8</c> for a document that
        /// <see cref="XmlDocument"/> parsed, because no other encoding is accepted.
        /// </value>
        public string Encoding { get; }

        /// <summary>
        /// Gets the value of the <c>standalone</c> pseudo-attribute of the declaration.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the declaration says <c>standalone="yes"</c>; <see langword="false"/> if
        /// it says <c>standalone="no"</c>; <see langword="null"/> if the pseudo-attribute is omitted.
        /// </value>
        public bool? Standalone { get; }

        /// <summary>
        /// Gets the XML version named in the declaration.
        /// </summary>
        /// <value>
        /// The version string, which is <c>1.0</c> or <c>1.1</c> for a document that
        /// <see cref="XmlDocument"/> parsed.
        /// </value>
        public string Version { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlDeclarationInfo"/> struct.
        /// </summary>
        /// <param name="version">The XML version named in the declaration, such as <c>1.0</c>.</param>
        /// <param name="encoding">The name of the encoding named in the declaration, such as <c>utf-8</c>.</param>
        /// <param name="standalone">The value of the <c>standalone</c> pseudo-attribute, or <see langword="null"/> if it is omitted.</param>
        public XmlDeclarationInfo(string version, string encoding, bool? standalone)
        {
            Version = version;
            Encoding = encoding;
            Standalone = standalone;
        }

        #endregion

    }

}
