using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that marks a value type with no setter to be populated.
    /// </summary>
    public sealed class PopulateBadStruct
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the member that cannot be populated.
        /// </summary>
        [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)]
        public PopulatePoint Point { get; } = new() { X = 7 };

        #endregion

    }

}
