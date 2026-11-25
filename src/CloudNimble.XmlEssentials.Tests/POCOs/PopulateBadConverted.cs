using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that marks a member whose type has its own converter to be populated.
    /// </summary>
    public sealed class PopulateBadConverted
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the member that cannot be populated.
        /// </summary>
        [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)]
        public Money Price { get; set; }

        #endregion

    }

}
