using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// The document element of a podcast feed: <c>&lt;rss version="2.0"&gt;</c> with one channel.
    /// </summary>
    /// <remarks>
    /// The members are in alphabetical order. <see cref="XmlPropertyOrderAttribute"/> gives each the place it
    /// has in the feed.
    /// </remarks>
    [XmlRoot("rss")]
    public sealed class PodcastRss
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the channel: the show and its episodes.
        /// </summary>
        [XmlElement("channel")]
        [XmlPropertyOrder(1)]
        public PodcastChannel? Channel { get; set; }

        /// <summary>
        /// Gets or sets the RSS version.
        /// </summary>
        [XmlAttribute("version")]
        [XmlPropertyOrder(0)]
        public string? Version { get; set; }

        #endregion

    }

}
