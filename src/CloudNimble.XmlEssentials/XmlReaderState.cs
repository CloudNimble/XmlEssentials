namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Opaque resume state for <see cref="Utf8XmlReader"/>.
    /// </summary>
    /// <remarks>
    /// The public surface matches <c>Utf8JsonReader</c>: only <see cref="Options"/> is visible.
    /// <see cref="Utf8XmlReader.CurrentState"/> copies an immutable snapshot so two readers
    /// never share mutable element or namespace stacks.
    /// </remarks>
    public readonly struct XmlReaderState
    {

        #region Public Properties

        /// <summary>
        /// Gets the options used to create the reader.
        /// </summary>
        public XmlReaderOptions Options { get; }

        /// <summary>
        /// Gets the immutable parse snapshot, if any.
        /// </summary>
        /// <remarks>
        /// Null when the reader has not yet consumed a token. Copied on read so two
        /// <see cref="Utf8XmlReader"/> values never share mutable stacks.
        /// </remarks>
        internal XmlReaderStateSnapshot? Snapshot { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a state with the given options and no parse progress.
        /// </summary>
        /// <param name="options">Reader options.</param>
        public XmlReaderState(XmlReaderOptions options = default)
        {
            Options = options;
            Snapshot = null;
        }

        /// <summary>
        /// Creates a state with options and a snapshot.
        /// </summary>
        /// <param name="options">Reader options.</param>
        /// <param name="snapshot">The immutable snapshot.</param>
        /// <remarks>
        /// Used by <see cref="Utf8XmlReader.CurrentState"/>. The snapshot is treated as immutable.
        /// </remarks>
        internal XmlReaderState(XmlReaderOptions options, XmlReaderStateSnapshot snapshot)
        {
            Options = options;
            Snapshot = snapshot;
        }

        #endregion

    }

}
