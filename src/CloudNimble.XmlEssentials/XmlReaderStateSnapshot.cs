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
        /// Gets the namespace URI of the current start or end element token.
        /// </summary>
        /// <remarks>
        /// Stored separately from the element stack because an end tag pops its scope
        /// before the caller inspects <see cref="Utf8XmlReader.NamespaceUriSpan"/>.
        /// </remarks>
        public required string CurrentNamespaceUri { get; init; }

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
        /// Gets the prefix each open element was started with, or an empty string. An end tag must repeat it.
        /// </summary>
        public required string[] ElementPrefixes { get; init; }

        /// <summary>
        /// Gets, per open element, whether it has reported character data, has a child element,
        /// or is under <c>xml:space="preserve"</c>. Whitespace handling after a resume depends on all three.
        /// </summary>
        public required int[] ElementFlags { get; init; }

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
        /// Gets whether the reader is on the start tag of an element that is now open, which is reported one
        /// level above its content.
        /// </summary>
        public required bool IsOnOpenStartTag { get; init; }

        /// <summary>
        /// Gets the 1-based line number.
        /// </summary>
        public required long LineNumber { get; init; }

        /// <summary>
        /// Gets the owning element depth of each namespace binding.
        /// </summary>
        public required int[] NamespaceDepths { get; init; }

        /// <summary>
        /// Gets the prefix of each namespace binding. Empty is the default xmlns.
        /// </summary>
        public required string[] NamespacePrefixes { get; init; }

        /// <summary>
        /// Gets the URI of each namespace binding. Empty is unbound.
        /// </summary>
        public required string[] NamespaceUris { get; init; }

        /// <summary>
        /// Gets the byte offset of the current line start.
        /// </summary>
        public required long LineStart { get; init; }

        /// <summary>
        /// Gets the namespace depth to pop after leaving an empty element, or 0.
        /// </summary>
        public required int PendingEmptyNsDepth { get; init; }

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

        /// <summary>
        /// Gets whether an XML declaration is still legal at the next token.
        /// </summary>
        /// <remarks>
        /// True only before the first token is completed. An incomplete declaration on a
        /// non-final buffer must keep this true so resume can finish <c>&lt;?xml ...?&gt;</c>.
        /// </remarks>
        public required bool XmlDeclarationAllowed { get; init; }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns this snapshot moved to another place in the document.
        /// </summary>
        /// <param name="documentBytesConsumed">The document offset of the new place.</param>
        /// <param name="lineNumber">The line number there.</param>
        /// <param name="lineStart">The line start there.</param>
        /// <returns>A snapshot that differs only in position.</returns>
        /// <remarks>
        /// Two places in a document have the same snapshot apart from position when the same elements are open
        /// and the same namespaces are in scope at both: the start of each child of one element, for example.
        /// <c>XmlSerializer.DeserializeAsyncEnumerable</c> captures the state at the first child once and moves
        /// it to each later one, instead of capturing a new snapshot per value.
        /// </remarks>
        internal XmlReaderStateSnapshot At(long documentBytesConsumed, long lineNumber, long lineStart)
        {
            return new XmlReaderStateSnapshot
            {
                CurrentNamespaceUri = CurrentNamespaceUri,
                Depth = Depth,
                DocumentBytesConsumed = documentBytesConsumed,
                ElementLocalNames = ElementLocalNames,
                ElementPrefixes = ElementPrefixes,
                ElementFlags = ElementFlags,
                ElementNamespaceUris = ElementNamespaceUris,
                Finished = Finished,
                IsEmptyElement = IsEmptyElement,

                // The new position is after a complete token, so it is inside the open elements, not on a start tag.
                IsOnOpenStartTag = false,
                LineNumber = lineNumber,
                LineStart = lineStart,
                NamespaceDepths = NamespaceDepths,
                NamespacePrefixes = NamespacePrefixes,
                NamespaceUris = NamespaceUris,
                PendingEmptyNsDepth = PendingEmptyNsDepth,
                SeenRoot = SeenRoot,
                TokenType = TokenType,
                Version = Version,
                XmlDeclarationAllowed = XmlDeclarationAllowed,
            };
        }

        #endregion

    }

}
