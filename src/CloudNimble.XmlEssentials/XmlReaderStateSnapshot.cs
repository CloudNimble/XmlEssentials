namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Immutable parse snapshot copied on <see cref="Utf8XmlReader.CurrentState"/>.
    /// </summary>
    /// <remarks>
    /// Two readers must never share this instance mutably. <see cref="Utf8XmlReader"/> copies
    /// stacks into new arrays on capture so a resumed reader cannot alias the source.
    /// </remarks>
    internal sealed class XmlReaderStateSnapshot
    {

        #region Public Properties

        /// <summary>
        /// Gets the element depth.
        /// </summary>
        public required int Depth { get; init; }

        /// <summary>
        /// Gets the document bytes consumed before the current buffer.
        /// </summary>
        public required long DocumentBytesConsumed { get; init; }

        /// <summary>
        /// Gets the element local-name stack.
        /// </summary>
        public required string[] ElementLocalNames { get; init; }

        /// <summary>
        /// Gets the element namespace-URI stack.
        /// </summary>
        public required string[] ElementNamespaceUris { get; init; }

        /// <summary>
        /// Gets whether the document is finished.
        /// </summary>
        public required bool Finished { get; init; }

        /// <summary>
        /// Gets whether the current start tag is empty.
        /// </summary>
        public required bool IsEmptyElement { get; init; }

        /// <summary>
        /// Gets the 1-based line number.
        /// </summary>
        public required long LineNumber { get; init; }

        /// <summary>
        /// Gets the byte offset of the current line start.
        /// </summary>
        public required long LineStart { get; init; }

        /// <summary>
        /// Gets whether a root element has been seen.
        /// </summary>
        public required bool SeenRoot { get; init; }

        /// <summary>
        /// Gets the current token type.
        /// </summary>
        public required XmlTokenType TokenType { get; init; }

        /// <summary>
        /// Gets the XML version in force.
        /// </summary>
        public required XmlVersion Version { get; init; }

        #endregion

    }

}
