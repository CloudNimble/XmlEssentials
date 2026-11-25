using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that holds the same kind of text in an element and in an attribute.
    /// </summary>
    public sealed class TextHolder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the text that is written as an element.
        /// </summary>
        public string Body { get; set; } = "";

        /// <summary>
        /// Gets or sets the text that is written as an attribute.
        /// </summary>
        [XmlAttribute]
        public string Label { get; set; } = "";

        #endregion

    }

}
