using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that maps a nested object as an XML attribute, which is illegal.
    /// </summary>
    public sealed class AttributeOnComplex
    {

        #region Public Properties

        /// <summary>
        /// Gets an address that cannot be an attribute.
        /// </summary>
        [XmlAttribute]
        public Address? Home { get; set; }

        #endregion

    }

}
