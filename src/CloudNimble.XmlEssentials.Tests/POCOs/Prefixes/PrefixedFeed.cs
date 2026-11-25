using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Prefixes
{

    /// <summary>
    /// A document type that states the prefixes of its two namespaces, and uses them on an attribute, on its
    /// own elements, and on the elements of a child.
    /// </summary>
    [XmlRoot("feed")]
    [XmlNamespaceDeclaration("a", "urn:a")]
    [XmlNamespaceDeclaration("b", "urn:b")]
    public sealed class PrefixedFeed
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a child element in <c>urn:b</c>.
        /// </summary>
        [XmlElement("entry", "urn:b")]
        public PrefixedEntry? Entry { get; set; }

        /// <summary>
        /// Gets or sets an attribute in <c>urn:b</c>.
        /// </summary>
        [XmlAttribute("id", "urn:b")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets an element in <c>urn:a</c>.
        /// </summary>
        [XmlElement("x", "urn:a")]
        [XmlPropertyOrder(0)]
        public string? X { get; set; }

        /// <summary>
        /// Gets or sets an element with no namespace.
        /// </summary>
        [XmlElement("y")]
        [XmlPropertyOrder(1)]
        public string? Y { get; set; }

        #endregion

    }

}
