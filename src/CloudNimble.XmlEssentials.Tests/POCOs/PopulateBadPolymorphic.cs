using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that marks a member of a polymorphic type to be populated.
    /// </summary>
    public sealed class PopulateBadPolymorphic
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the member that cannot be populated.
        /// </summary>
        [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)]
        public FallbackProduct Product { get; set; } = new FallbackBook();

        #endregion

    }

}
