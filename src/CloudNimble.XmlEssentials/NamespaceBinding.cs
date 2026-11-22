namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// One xmlns binding in the in-progress document.
    /// </summary>
    /// <remarks>
    /// Prefix and URI are either slices of the current buffer (<see cref="PrefixStart"/> /
    /// <see cref="UriStart"/> ≥ 0) or owned copies for resume. An empty prefix is the default
    /// xmlns. An empty URI undeclares that prefix (1.1) or the default namespace.
    /// </remarks>
    internal readonly struct NamespaceBinding
    {

        #region Public Properties

        /// <summary>
        /// Gets the element depth that owns this binding.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Gets the owned prefix bytes when <see cref="PrefixStart"/> is negative.
        /// </summary>
        public byte[] PrefixOwned { get; }

        /// <summary>
        /// Gets the prefix byte offset in the current buffer, or -1 when owned.
        /// </summary>
        public int PrefixStart { get; }

        /// <summary>
        /// Gets the prefix UTF-8 byte length.
        /// </summary>
        public int PrefixLength { get; }

        /// <summary>
        /// Gets the owned URI bytes when <see cref="UriStart"/> is negative.
        /// </summary>
        public byte[] UriOwned { get; }

        /// <summary>
        /// Gets the URI byte offset in the current buffer, or -1 when owned.
        /// </summary>
        public int UriStart { get; }

        /// <summary>
        /// Gets the URI UTF-8 byte length.
        /// </summary>
        public int UriLength { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an owned binding (resume / interned).
        /// </summary>
        /// <param name="depth">The owning element depth.</param>
        /// <param name="prefix">The prefix bytes. Empty for the default xmlns.</param>
        /// <param name="uri">The URI bytes. Empty when unbound.</param>
        public NamespaceBinding(int depth, byte[] prefix, byte[] uri)
        {
            Depth = depth;
            PrefixOwned = prefix;
            PrefixStart = -1;
            PrefixLength = prefix.Length;
            UriOwned = uri;
            UriStart = -1;
            UriLength = uri.Length;
        }

        /// <summary>
        /// Creates a buffer-backed binding.
        /// </summary>
        /// <param name="depth">The owning element depth.</param>
        /// <param name="prefixStart">Prefix offset in the current buffer, or -1 when owned.</param>
        /// <param name="prefixLength">Prefix UTF-8 length.</param>
        /// <param name="prefixOwned">Owned prefix when <paramref name="prefixStart"/> is -1.</param>
        /// <param name="uriStart">URI offset in the current buffer, or -1 when owned.</param>
        /// <param name="uriLength">URI UTF-8 length.</param>
        /// <param name="uriOwned">Owned URI when <paramref name="uriStart"/> is -1.</param>
        public NamespaceBinding(int depth, int prefixStart, int prefixLength, byte[] prefixOwned, int uriStart, int uriLength, byte[] uriOwned)
        {
            Depth = depth;
            PrefixStart = prefixStart;
            PrefixLength = prefixLength;
            PrefixOwned = prefixOwned;
            UriStart = uriStart;
            UriLength = uriLength;
            UriOwned = uriOwned;
        }

        #endregion

    }

}
