using Bcl = System.Xml.Serialization;
using Xe = CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// One episode of a <see cref="Feed"/>.
    /// </summary>
    public sealed class FeedItem
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [Bcl.XmlElement("description")]
        [Xe.XmlElement("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the length in seconds.
        /// </summary>
        [Bcl.XmlElement("duration", Namespace = Feed.ITunes)]
        [Xe.XmlElement("duration", Feed.ITunes)]
        public int Duration { get; set; }

        /// <summary>
        /// Gets or sets the full content.
        /// </summary>
        [Bcl.XmlElement("encoded", Namespace = Feed.Content)]
        [Xe.XmlElement("encoded", Feed.Content)]
        public string? Encoded { get; set; }

        /// <summary>
        /// Gets or sets the episode number.
        /// </summary>
        [Bcl.XmlElement("episode", Namespace = Feed.ITunes)]
        [Xe.XmlElement("episode", Feed.ITunes)]
        public int Episode { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [Bcl.XmlElement("guid")]
        [Xe.XmlElement("guid")]
        public string? Guid { get; set; }

        /// <summary>
        /// Gets or sets the iTunes title.
        /// </summary>
        [Bcl.XmlElement("title", Namespace = Feed.ITunes)]
        [Xe.XmlElement("title", Feed.ITunes)]
        public string? ITunesTitle { get; set; }

        /// <summary>
        /// Gets or sets the link.
        /// </summary>
        [Bcl.XmlElement("link")]
        [Xe.XmlElement("link")]
        public string? Link { get; set; }

        /// <summary>
        /// Gets or sets the publication date, as the feed spells it.
        /// </summary>
        [Bcl.XmlElement("pubDate")]
        [Xe.XmlElement("pubDate")]
        public string? Published { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [Bcl.XmlElement("title")]
        [Xe.XmlElement("title")]
        public string? Title { get; set; }

        #endregion

    }

}
