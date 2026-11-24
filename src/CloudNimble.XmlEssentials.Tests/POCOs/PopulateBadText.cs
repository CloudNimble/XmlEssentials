using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that marks a string to be populated.
    /// </summary>
    public sealed class PopulateBadText
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the member that cannot be populated.
        /// </summary>
        [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)]
        public string Name { get; set; } = "n";

        #endregion

    }

}
