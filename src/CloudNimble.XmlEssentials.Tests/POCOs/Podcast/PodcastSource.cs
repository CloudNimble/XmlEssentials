using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// A <c>podcast:source</c>: where an alternate enclosure can be fetched from.
    /// </summary>
    /// <remarks>
    /// The members are in alphabetical order. <see cref="XmlPropertyOrderAttribute"/> gives each the place it
    /// has in the feed.
    /// </remarks>
    public sealed class PodcastSource
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        [XmlAttribute("uri")]
        [XmlPropertyOrder(0)]
        public string? Uri { get; set; }

        #endregion

    }

}
