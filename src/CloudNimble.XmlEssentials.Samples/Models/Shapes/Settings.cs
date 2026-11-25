using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// Application settings that keep the attributes and elements this version of the type does not map.
    /// </summary>
    public sealed class Settings
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the theme name.
        /// </summary>
        public string? Theme { get; set; }

        /// <summary>
        /// Gets or sets the attributes and elements that no other member maps.
        /// </summary>
        [XmlExtensionData]
        public XmlExtensionData? Extra { get; set; }

        #endregion

    }

}
