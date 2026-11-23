using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose title is required by <see cref="XmlRequiredAttribute"/>.
    /// </summary>
    public sealed class RequiredTitle
    {

        #region Public Properties

        /// <summary>
        /// Gets the required title.
        /// </summary>
        [XmlRequired]
        public string? Title { get; set; }

        #endregion

    }

}
