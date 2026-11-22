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

    }

}
