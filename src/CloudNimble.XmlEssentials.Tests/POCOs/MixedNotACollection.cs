using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A mixed-content member that is not a collection, which is refused.
    /// </summary>
    public sealed class MixedNotACollection
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        [XmlMixed]
        public string? Content { get; set; }

        #endregion

    }

}