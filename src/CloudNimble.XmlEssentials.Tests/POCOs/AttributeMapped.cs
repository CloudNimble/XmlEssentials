using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that maps a member as an XML attribute.
    /// </summary>
    public sealed class AttributeMapped
    {

        #region Public Properties

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        [XmlAttribute]
        public int Id { get; set; }

        #endregion

    }

}
