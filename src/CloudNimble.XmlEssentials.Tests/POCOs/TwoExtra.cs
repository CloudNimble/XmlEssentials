using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with two extension-data members, which is illegal.
    /// </summary>
    public sealed class TwoExtra
    {

        #region Public Properties

        /// <summary>
        /// Gets the first bag.
        /// </summary>
        [XmlExtensionData]
        public XmlExtensionData A { get; set; } = new();

        /// <summary>
        /// Gets the second bag.
        /// </summary>
        [XmlExtensionData]
        public XmlExtensionData B { get; set; } = new();

        #endregion

    }

}
