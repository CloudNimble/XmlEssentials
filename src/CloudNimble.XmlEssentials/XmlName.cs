namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// A namespace-qualified XML name: local name, namespace URI, and optional prefix.
    /// </summary>
    /// <remarks>
    /// Equality is the ordinal combination of <see cref="LocalName"/> and <see cref="NamespaceUri"/>.
    /// <see cref="Prefix"/> is serialization sugar and is not significant for equality or mapping.
    /// </remarks>
    public readonly struct XmlName : IEquatable<XmlName>
    {

        #region Public Properties

        /// <summary>
        /// Gets the local name.
        /// </summary>
        public string LocalName { get; }

        /// <summary>
        /// Gets the namespace URI, or an empty string when the name is in no namespace.
        /// </summary>
        public string NamespaceUri { get; }

        /// <summary>
        /// Gets the prefix, or an empty string when the name is unprefixed.
        /// </summary>
        public string Prefix { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a qualified name.
        /// </summary>
        /// <param name="localName">The local name. Cannot be null or whitespace.</param>
        /// <param name="namespaceUri">The namespace URI, or null for no namespace.</param>
        /// <param name="prefix">The prefix, or null for none. Not used in equality.</param>
        /// <example>
        /// <code>
        /// var name = new XmlName("Person", "https://example.com", "e");
        /// Console.WriteLine(name); // e:Person
        /// </code>
        /// </example>
        /// <remarks>
        /// Pass a namespace URI without a prefix when comparing names. Prefix is only written
        /// by <see cref="ToString"/> when it is non-empty.
        /// </remarks>
        public XmlName(string localName, string? namespaceUri = null, string? prefix = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localName);
            LocalName = localName;
            NamespaceUri = namespaceUri ?? "";
            Prefix = prefix ?? "";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns true when <paramref name="other"/> has the same local name and namespace URI.
        /// </summary>
        /// <param name="other">The name to compare.</param>
        /// <returns><see langword="true"/> if the names are equal.</returns>
        public bool Equals(XmlName other)
        {
            return string.Equals(LocalName, other.LocalName, StringComparison.Ordinal)
                && string.Equals(NamespaceUri, other.NamespaceUri, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns true when <paramref name="obj"/> is an <see cref="XmlName"/> equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><see langword="true"/> if the names are equal.</returns>
        public override bool Equals(object? obj)
        {
            return obj is XmlName other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code over <see cref="LocalName"/> and <see cref="NamespaceUri"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(LocalName, NamespaceUri);
        }

        /// <summary>
        /// Returns <c>prefix:local</c> when a prefix is set, <c>{uri}local</c> when a namespace is set,
        /// otherwise the local name.
        /// </summary>
        /// <returns>A display form of the name.</returns>
        public override string ToString()
        {
            if (Prefix.Length > 0)
            {
                return string.Create(Prefix.Length + 1 + LocalName.Length, this, static (destination, name) =>
                {
                    name.Prefix.AsSpan().CopyTo(destination);
                    destination[name.Prefix.Length] = ':';
                    name.LocalName.AsSpan().CopyTo(destination[(name.Prefix.Length + 1)..]);
                });
            }

            if (NamespaceUri.Length > 0)
            {
                return string.Create(NamespaceUri.Length + LocalName.Length + 2, this, static (destination, name) =>
                {
                    destination[0] = '{';
                    name.NamespaceUri.AsSpan().CopyTo(destination[1..]);
                    destination[name.NamespaceUri.Length + 1] = '}';
                    name.LocalName.AsSpan().CopyTo(destination[(name.NamespaceUri.Length + 2)..]);
                });
            }

            return LocalName;
        }

        /// <summary>
        /// Returns true when <paramref name="left"/> and <paramref name="right"/> are equal.
        /// </summary>
        /// <param name="left">The first name.</param>
        /// <param name="right">The second name.</param>
        /// <returns><see langword="true"/> if the names are equal.</returns>
        public static bool operator ==(XmlName left, XmlName right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns true when <paramref name="left"/> and <paramref name="right"/> are not equal.
        /// </summary>
        /// <param name="left">The first name.</param>
        /// <param name="right">The second name.</param>
        /// <returns><see langword="true"/> if the names differ.</returns>
        public static bool operator !=(XmlName left, XmlName right)
        {
            return !left.Equals(right);
        }

        #endregion

    }

}
