using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Declares a namespace prefix on the document element when the type is serialized as a document, so that
    /// elements and attributes in that namespace are written with the prefix.
    /// </summary>
    /// <example>
    /// <code>
    /// [XmlRoot("rss")]
    /// [XmlNamespaceDeclaration("atom", "http://www.w3.org/2005/Atom")]
    /// [XmlNamespaceDeclaration("itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd")]
    /// public sealed class Feed
    /// {
    ///     [XmlElement("author", "http://www.itunes.com/dtds/podcast-1.0.dtd")]
    ///     public string? Author { get; set; }
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>
    /// The attribute can be applied more than once. The declarations are written in the order they appear, after
    /// those in <see cref="XmlSerializerOptions.Namespaces"/>. A declaration is left out when the options already
    /// declare its prefix or its namespace: the options take precedence.
    /// </para>
    /// <para>
    /// The attribute affects writing only. When the serializer reads, it compares namespace URIs and never
    /// prefixes. It has no effect on a type that is serialized as a member of another type.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class XmlNamespaceDeclarationAttribute : Attribute
    {

        #region Properties

        /// <summary>
        /// Gets the namespace URI that <see cref="Prefix"/> is bound to.
        /// </summary>
        /// <value>
        /// The namespace URI.
        /// </value>
        public string NamespaceUri { get; }

        /// <summary>
        /// Gets the prefix that is declared.
        /// </summary>
        /// <value>
        /// The prefix, or an empty string for the default namespace.
        /// </value>
        public string Prefix { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlNamespaceDeclarationAttribute"/> class with a prefix
        /// and the namespace URI it is bound to.
        /// </summary>
        /// <param name="prefix">The prefix to declare, or an empty string to declare the default namespace.</param>
        /// <param name="namespaceUri">The namespace URI to bind to <paramref name="prefix"/>.</param>
        /// <remarks>
        /// The values are checked when the type is first serialized or deserialized. A prefix that is not an XML
        /// name without a colon, the <c>xml</c> and <c>xmlns</c> prefixes, their namespaces, and an empty
        /// namespace URI cause an <see cref="InvalidOperationException"/> then.
        /// </remarks>
        public XmlNamespaceDeclarationAttribute(string prefix, string namespaceUri)
        {
            Prefix = prefix;
            NamespaceUri = namespaceUri;
        }

        #endregion

    }

}
