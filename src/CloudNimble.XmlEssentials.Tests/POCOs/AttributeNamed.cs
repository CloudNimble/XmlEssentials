using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that maps a named XML attribute.
    /// </summary>
    public sealed class AttributeNamed
    {

        #region Public Properties

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        [XmlAttribute("id", "urn:x")]
        public int Id { get; set; }

        #endregion

    }

}
