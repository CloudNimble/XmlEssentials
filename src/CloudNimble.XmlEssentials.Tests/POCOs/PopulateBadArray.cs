using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that marks an array to be populated.
    /// </summary>
    public sealed class PopulateBadArray
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the member that cannot be populated.
        /// </summary>
        [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)]
        public int[] Items { get; set; } = [1];

        #endregion

    }

}
