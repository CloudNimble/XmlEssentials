using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds, as an attribute and as text, a type whose converter comes from the options.
    /// </summary>
    public sealed class Parcel
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the weight when empty, written as an attribute.
        /// </summary>
        [XmlAttribute]
        public Weight Tare { get; set; }

        /// <summary>
        /// Gets or sets the weight when full, written as the text of the element.
        /// </summary>
        [XmlText]
        public Weight Gross { get; set; }

        #endregion

    }

}
