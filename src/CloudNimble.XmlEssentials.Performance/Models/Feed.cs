using Bcl = System.Xml.Serialization;
using Xe = CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// A podcast feed: elements in no namespace beside elements of the same local name in other namespaces.
    /// </summary>
    [Bcl.XmlRoot("rss")]
    [Xe.XmlRoot("rss")]
    public sealed class Feed
    {

        #region Fields

        /// <summary>
        /// The Atom namespace.
        /// </summary>
        public const string Atom = "http://www.w3.org/2005/Atom";

        /// <summary>
        /// The RSS content module namespace.
        /// </summary>
        public const string Content = "http://purl.org/rss/1.0/modules/content/";

        /// <summary>
        /// The iTunes podcast namespace.
        /// </summary>
        public const string ITunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the channel.
        /// </summary>
        [Bcl.XmlElement("channel")]
        [Xe.XmlElement("channel")]
        public FeedChannel? Channel { get; set; }

        /// <summary>
        /// Gets or sets the RSS version.
        /// </summary>
        [Bcl.XmlAttribute("version")]
        [Xe.XmlAttribute("version")]
        public string? Version { get; set; }

        #endregion

    }

}
