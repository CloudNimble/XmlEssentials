using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose extension-data bag starts null.
    /// </summary>
    public sealed class NullExtra
    {

        #region Public Properties

        /// <summary>
        /// Gets unmapped members.
        /// </summary>
        [XmlExtensionData]
        public XmlExtensionData? Extra { get; set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
