using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds values of a type with its own converter as attributes, as an element, and as the text of the element.
    /// </summary>
    public sealed class Priced
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a converted value written as an attribute.
        /// </summary>
        [XmlAttribute]
        public Money Price { get; set; }

        /// <summary>
        /// Gets or sets a converted value that can be null, written as an attribute.
        /// </summary>
        [XmlAttribute]
        public Money? Sale { get; set; }

        /// <summary>
        /// Gets or sets a converted value written as the text of the element.
        /// </summary>
        [XmlText]
        public Money Total { get; set; }

        #endregion

    }

}
