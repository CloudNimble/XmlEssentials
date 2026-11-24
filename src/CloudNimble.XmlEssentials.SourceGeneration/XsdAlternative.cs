using System;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// One element a member generated from a schema may hold: an alternative of an <c>xs:choice</c>, an element of a
    /// repeating group, or a member of a substitution group.
    /// </summary>
    internal sealed class XsdAlternative : IEquatable<XsdAlternative>
    {

        #region Public Properties

        /// <summary>
        /// Gets the fully qualified CLR type of the element's value.
        /// </summary>
        internal string ClrTypeName { get; }

        /// <summary>
        /// Gets the XML name of the element.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Gets the namespace of the element: a URI, an empty string for no namespace, or <see langword="null"/>
        /// when the element takes the default namespace in scope.
        /// </summary>
        internal string? Namespace { get; }

        /// <summary>
        /// Gets the name of the named complex type of the element, or <see langword="null"/> when it has another type.
        /// </summary>
        internal string? TypeName { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an alternative.
        /// </summary>
        /// <param name="name">The XML name of the element.</param>
        /// <param name="namespaceUri">The namespace of the element, empty for none, or <see langword="null"/>.</param>
        /// <param name="clrTypeName">The fully qualified CLR type of the value.</param>
        /// <param name="typeName">The name of the named complex type, or <see langword="null"/>.</param>
        internal XsdAlternative(string name, string? namespaceUri, string clrTypeName, string? typeName)
        {
            Name = name;
            Namespace = namespaceUri;
            ClrTypeName = clrTypeName;
            TypeName = typeName;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public bool Equals(XsdAlternative? other)
        {
            return other is not null
                && Name == other.Name
                && Namespace == other.Namespace
                && ClrTypeName == other.ClrTypeName
                && TypeName == other.TypeName;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as XsdAlternative);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ ClrTypeName.GetHashCode();
        }

        #endregion

    }

}
