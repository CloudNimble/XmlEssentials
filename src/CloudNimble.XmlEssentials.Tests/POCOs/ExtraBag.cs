using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with an extension-data bag.
    /// </summary>
    public sealed class ExtraBag
    {

        #region Public Properties

        /// <summary>
        /// Gets unmapped members.
        /// </summary>
        [XmlExtensionData]
        public XmlExtensionData Extra { get; set; } = new();

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
