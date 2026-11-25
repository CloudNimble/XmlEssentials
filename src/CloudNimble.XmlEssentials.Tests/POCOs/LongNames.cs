using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a one-letter root name and members whose names are longer.
    /// </summary>
    [XmlRoot("r")]
    public sealed class LongNames
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value written as an attribute with a long name, or null to leave it out.
        /// </summary>
        [XmlAttribute]
        public string? LongAttribute { get; set; }

        /// <summary>
        /// Gets or sets a value written as an element with a long name, or null to leave it out.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingNull)]
        public string? LongElement { get; set; }

        #endregion

    }

}
