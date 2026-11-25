using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose extension data has no setter and starts out null, so unmapped members have nowhere to go.
    /// </summary>
    public sealed class LazyExtra
    {

        #region Public Properties

        /// <summary>
        /// Gets the unmapped members. Always null.
        /// </summary>
        [XmlExtensionData]
        public XmlExtensionData? Extra { get; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
