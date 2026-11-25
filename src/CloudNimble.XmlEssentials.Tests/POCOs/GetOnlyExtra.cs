using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose extension-data bag is get-only and initialized.
    /// </summary>
    public sealed class GetOnlyExtra
    {

        #region Public Properties

        /// <summary>
        /// Gets unmapped members.
        /// </summary>
        [XmlExtensionData]
        public XmlExtensionData Extra { get; } = new();

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
