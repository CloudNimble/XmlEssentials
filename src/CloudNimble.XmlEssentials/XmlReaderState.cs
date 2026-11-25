namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Represents the state a <see cref="Utf8XmlReader"/> needs to continue reading a document with more data.
    /// </summary>
    /// <remarks>
    /// The state is opaque. As with the state of a <c>Utf8JsonReader</c>, only <see cref="Options"/> is
    /// visible. <see cref="Utf8XmlReader.CurrentState"/> copies an immutable snapshot so two readers
    /// never share mutable element or namespace stacks.
    /// </remarks>
    public readonly struct XmlReaderState
    {

        #region Public Properties

        /// <summary>
        /// Gets the custom behavior of the <see cref="Utf8XmlReader"/> that this state belongs to.
        /// </summary>
        /// <value>
        /// The options the reader was created with.
        /// </value>
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
        /// Initializes a new instance of the <see cref="XmlReaderState"/> struct using the specified options.
        /// </summary>
        /// <param name="options">
        /// The options that define the behavior of the <see cref="Utf8XmlReader"/>. The default is
        /// <see langword="default"/>, which is the same as <c>new XmlReaderOptions()</c>: comments and
        /// whitespace-only text between elements are reported as tokens.
        /// </param>
        /// <remarks>
        /// The new state holds no read progress, so a reader created from it starts at the beginning of a document.
        /// </remarks>
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
