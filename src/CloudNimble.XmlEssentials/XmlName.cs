using System;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Represents a namespace-qualified XML name: a local name, a namespace URI, and an optional prefix.
    /// </summary>
    /// <remarks>
    /// Two names are equal when <see cref="LocalName"/> and <see cref="NamespaceUri"/> are equal by ordinal comparison.
    /// <see cref="Prefix"/> only affects how the name is written. It is not used for equality or for mapping.
    /// </remarks>
    public readonly struct XmlName : IEquatable<XmlName>
    {

        #region Public Properties

        /// <summary>
        /// Gets the local part of the name.
        /// </summary>
        /// <value>
        /// The local name.
        /// </value>
        public string LocalName { get; }

        /// <summary>
        /// Gets the namespace URI of the name.
        /// </summary>
        /// <value>
        /// The namespace URI, or an empty string if the name is in no namespace.
        /// </value>
        public string NamespaceUri { get; }

        /// <summary>
        /// Gets the prefix of the name.
        /// </summary>
        /// <value>
        /// The prefix, or an empty string if the name has no prefix.
        /// </value>
        public string Prefix { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlName"/> struct with the specified local name, namespace URI, and prefix.
        /// </summary>
        /// <param name="localName">The local name. It cannot be <see langword="null"/>, empty, or white space.</param>
        /// <param name="namespaceUri">The namespace URI, or <see langword="null"/> for no namespace. The default is <see langword="null"/>.</param>
        /// <param name="prefix">The prefix, or <see langword="null"/> for none. The default is <see langword="null"/>. The prefix is not used for equality.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="localName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="localName"/> is empty or consists only of white-space characters.
        /// </exception>
        /// <example>
        /// <code>
        /// var name = new XmlName("Person", "https://example.com", "e");
        /// Console.WriteLine(name); // e:Person
        /// </code>
        /// </example>
        /// <remarks>
        /// Pass a namespace URI without a prefix when the name is only compared with other names.
        /// <see cref="ToString"/> writes the prefix only when it is not empty.
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
        /// Determines whether this name and a specified <see cref="XmlName"/> have the same local name and namespace URI.
        /// </summary>
        /// <param name="other">The name to compare with this name.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="other"/> has the same local name and namespace URI as this name,
        /// by ordinal comparison; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(XmlName other)
        {
            return string.Equals(LocalName, other.LocalName, StringComparison.Ordinal)
                && string.Equals(NamespaceUri, other.NamespaceUri, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether this name and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare with this name.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="obj"/> is an <see cref="XmlName"/> with the same local name and
        /// namespace URI as this name; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Equals(object? obj)
        {
            return obj is XmlName other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this name, computed from <see cref="LocalName"/> and <see cref="NamespaceUri"/>.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(LocalName, NamespaceUri);
        }

        /// <summary>
        /// Returns a string that represents this name.
        /// </summary>
        /// <returns>
        /// <c>prefix:local</c> if the name has a prefix; otherwise, <c>{uri}local</c> if the name has a namespace URI;
        /// otherwise, the local name.
        /// </returns>
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
        /// Determines whether two <see cref="XmlName"/> values have the same local name and namespace URI.
        /// </summary>
        /// <param name="left">The first name to compare.</param>
        /// <param name="right">The second name to compare.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator ==(XmlName left, XmlName right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="XmlName"/> values have a different local name or namespace URI.
        /// </summary>
        /// <param name="left">The first name to compare.</param>
        /// <param name="right">The second name to compare.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator !=(XmlName left, XmlName right)
        {
            return !left.Equals(right);
        }

        #endregion

    }

}
