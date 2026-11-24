using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Prefixes
{

    /// <summary>
    /// A child of <see cref="PrefixedFeed"/> with an element in a namespace the document element declares.
    /// </summary>
    public sealed class PrefixedEntry
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets an element in <c>urn:a</c>.
        /// </summary>
        [XmlElement("z", "urn:a")]
        public string? Z { get; set; }

        #endregion

    }

}
