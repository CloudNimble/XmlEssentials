using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Prefixes
{

    /// <summary>
    /// A type that declares a namespace with no prefix given at all.
    /// </summary>
    [XmlNamespaceDeclaration(null!, "urn:x")]
    public sealed class NullPrefixDeclared
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value.
        /// </summary>
        public int N { get; set; }

        #endregion

    }

}
