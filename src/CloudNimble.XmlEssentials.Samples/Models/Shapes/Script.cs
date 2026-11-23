using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A script whose source code is written as a CDATA section.
    /// </summary>
    public sealed class Script
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the scripting language, written as an attribute.
        /// </summary>
        [XmlAttribute("language")]
        public string? Language { get; set; }

        /// <summary>
        /// Gets or sets the source code, written as the element's text in a CDATA section.
        /// </summary>
        [XmlCData]
        public string? Code { get; set; }

        #endregion

    }

}
