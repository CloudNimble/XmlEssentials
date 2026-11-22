namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Immutable XML declaration captured when an <see cref="XmlDocument"/> is parsed.
    /// </summary>
    /// <remarks>
    /// Mutable trees use <c>CloudNimble.Text.Xml.Nodes.XmlDeclarationNode</c> instead.
    /// </remarks>
    public readonly struct XmlDeclarationInfo
    {

        #region Public Properties

        /// <summary>
        /// Gets the declared encoding. Always <c>utf-8</c> for documents this stack accepts.
        /// </summary>
        public string Encoding { get; }

        /// <summary>
        /// Gets the standalone pseudo-attribute, or <see langword="null"/> when omitted.
        /// </summary>
        public bool? Standalone { get; }

        /// <summary>
        /// Gets the declared version, <c>1.0</c> or <c>1.1</c>.
        /// </summary>
        public string Version { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates declaration info.
        /// </summary>
        /// <param name="version">The version string.</param>
        /// <param name="encoding">The encoding string.</param>
        /// <param name="standalone">The standalone flag, or <see langword="null"/>.</param>
        public XmlDeclarationInfo(string version, string encoding, bool? standalone)
        {
            Version = version;
            Encoding = encoding;
            Standalone = standalone;
        }

        #endregion

    }

}
