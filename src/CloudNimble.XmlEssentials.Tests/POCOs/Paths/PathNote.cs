using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Paths
{

    /// <summary>
    /// An element with an attribute and a number as its text.
    /// </summary>
    public sealed class PathNote
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the language, an attribute.
        /// </summary>
        [XmlAttribute]
        public string? Lang { get; set; }

        /// <summary>
        /// Gets or sets the number, the text of the element.
        /// </summary>
        [XmlText]
        public int Number { get; set; }

        #endregion

    }

}
