namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// One xmlns binding in the in-progress document.
    /// </summary>
    /// <remarks>
    /// The record holds no object reference, so the reader's inline slots for it need not be cleared when a
    /// reader is constructed. A prefix or URI is a slice of the current buffer when its start is zero or
    /// greater. A negative start names bytes the reader owns: -1 is empty, and other values are resolved by
    /// <c>Utf8XmlReader.Owned</c> to a well-known namespace or an interned copy kept for resume. An empty
    /// prefix is the default xmlns. An empty URI undeclares that prefix (1.1) or the default namespace.
    /// </remarks>
    internal readonly struct NamespaceBinding
    {

        #region Public Properties

        /// <summary>
        /// Gets the element depth that owns this binding.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Gets the prefix length in bytes.
        /// </summary>
        public int PrefixLength { get; }

        /// <summary>
        /// Gets the prefix offset in the current buffer, or a negative value naming owned bytes.
        /// </summary>
        public int PrefixStart { get; }

        /// <summary>
        /// Gets the URI length in bytes.
        /// </summary>
        public int UriLength { get; }

        /// <summary>
        /// Gets the URI offset in the current buffer, or a negative value naming owned bytes.
        /// </summary>
        public int UriStart { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a binding.
        /// </summary>
        /// <param name="depth">The owning element depth.</param>
        /// <param name="prefixStart">The prefix offset, or a negative value naming owned bytes.</param>
        /// <param name="prefixLength">The prefix length.</param>
        /// <param name="uriStart">The URI offset, or a negative value naming owned bytes.</param>
        /// <param name="uriLength">The URI length.</param>
        public NamespaceBinding(int depth, int prefixStart, int prefixLength, int uriStart, int uriLength)
        {
            Depth = depth;
            PrefixStart = prefixStart;
            PrefixLength = prefixLength;
            UriStart = uriStart;
            UriLength = uriLength;
        }

        #endregion

    }

}
