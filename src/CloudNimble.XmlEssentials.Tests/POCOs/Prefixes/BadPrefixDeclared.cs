using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Prefixes
{

    /// <summary>
    /// A type that declares a prefix XML reserves.
    /// </summary>
    [XmlNamespaceDeclaration("xmlns", "urn:x")]
    public sealed class BadPrefixDeclared
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value.
        /// </summary>
        public int N { get; set; }

        #endregion

    }

}
