using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a nullable value mapped as an attribute in a namespace.
    /// </summary>
    public sealed class NamespacedOptionalCount
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the count, or null to leave the attribute out.
        /// </summary>
        [XmlAttribute("count", "urn:x")]
        public int? Count { get; set; }

        #endregion

    }

}
