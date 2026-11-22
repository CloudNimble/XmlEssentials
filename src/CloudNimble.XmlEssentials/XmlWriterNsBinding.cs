namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// One xmlns declaration written by <see cref="Utf8XmlWriter"/>.
    /// </summary>
    internal readonly struct XmlWriterNsBinding
    {

        #region Public Properties

        /// <summary>
        /// Gets the element depth that owns this declaration.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Gets the prefix, or empty for the default xmlns.
        /// </summary>
        public byte[] Prefix { get; }

        /// <summary>
        /// Gets the namespace URI, or empty when unbound.
        /// </summary>
        public byte[] Uri { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a writer namespace binding.
        /// </summary>
        /// <param name="depth">The owning element depth.</param>
        /// <param name="prefix">The prefix bytes.</param>
        /// <param name="uri">The URI bytes.</param>
        public XmlWriterNsBinding(int depth, byte[] prefix, byte[] uri)
        {
            Depth = depth;
            Prefix = prefix;
            Uri = uri;
        }

        #endregion

    }

}
