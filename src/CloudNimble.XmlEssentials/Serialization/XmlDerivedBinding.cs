using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// One derived type that can appear where a base member is expected.
    /// </summary>
    internal readonly struct XmlDerivedBinding
    {

        #region Public Properties

        /// <summary>
        /// Gets the type info of <see cref="Type"/>.
        /// </summary>
        internal XmlTypeInfo Info { get; }

        /// <summary>
        /// Gets the derived types of this type that an attribute identifies inside its element, when this is an
        /// alternative of a choice of elements; otherwise <see langword="null"/>.
        /// </summary>
        internal XmlPolymorphism? Nested { get; init; }

        /// <summary>
        /// Gets the derived CLR type.
        /// </summary>
        internal Type Type { get; }

        /// <summary>
        /// Gets the discriminator as written in an attribute or <c>xsi:type</c> value.
        /// </summary>
        internal string Discriminator { get; }

        /// <summary>
        /// Gets the UTF-8 discriminator element name.
        /// </summary>
        internal byte[] Utf8Name { get; }

        /// <summary>
        /// Gets the UTF-8 namespace of the element named for the derived type, or null when none was given and the
        /// element is written under its name alone.
        /// </summary>
        internal byte[]? Utf8NamespaceUri { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the binding.
        /// </summary>
        /// <param name="utf8Name">The discriminator local name.</param>
        /// <param name="type">The derived CLR type.</param>
        /// <param name="info">The type info.</param>
        /// <param name="discriminator">The discriminator string.</param>
        /// <param name="utf8NamespaceUri">The UTF-8 namespace of the element named for the derived type, or null when none was given.</param>
        internal XmlDerivedBinding(byte[] utf8Name, Type type, XmlTypeInfo info, string discriminator, byte[]? utf8NamespaceUri = null)
        {
            Utf8Name = utf8Name;
            Utf8NamespaceUri = utf8NamespaceUri;
            Type = type;
            Info = info;
            Discriminator = discriminator;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns whether the reader is on the element named for the derived type.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <param name="caseInsensitive">Whether ASCII case is ignored in the local name.</param>
        /// <returns><see langword="true"/> when the element is the one for this derived type.</returns>
        /// <remarks>
        /// The local name decides, as it does for a member. A derived type that was given a namespace is matched
        /// on the namespace as well, so two derived types can have one local name in two namespaces.
        /// </remarks>
        internal bool Matches(ref Utf8XmlReader reader, bool caseInsensitive)
        {
            return XmlBoundProperty.NameEquals(ref reader, Utf8Name, caseInsensitive)
                && (Utf8NamespaceUri is null || reader.NamespaceUriSpan.SequenceEqual(Utf8NamespaceUri));
        }

        /// <summary>
        /// Writes the attributes and content of a value inside the element of this derived type.
        /// </summary>
        /// <param name="writer">The writer, inside the start tag.</param>
        /// <param name="value">The value, of this type or of a type that derives from it.</param>
        /// <remarks>
        /// A value of a type that derives from an alternative of a choice is written with the discriminator the
        /// alternative's own type gives it.
        /// </remarks>
        internal void WriteContent(Utf8XmlWriter writer, object value)
        {
            if (Nested is { } nested && value.GetType() != Type && nested.TryGetDerivedWrite(value.GetType()) is { } inner)
            {
                XmlConverterHelpers.WriteDiscriminator(writer, nested.Placement, nested.Utf8DiscriminatorAttributeName, inner.Discriminator);
                inner.Info.WriteContentAsObject(writer, value);
                return;
            }

            Info.WriteContentAsObject(writer, value);
        }

        /// <summary>
        /// Writes the start tag of the element named for the derived type.
        /// </summary>
        /// <param name="writer">The writer.</param>
        internal void WriteStart(Utf8XmlWriter writer)
        {
            if (Utf8NamespaceUri is { } namespaceUri)
            {
                writer.WriteStartElement(Utf8Name, namespaceUri);
                return;
            }

            writer.WriteStartElement(Utf8Name);
        }
        #endregion

    }

}
