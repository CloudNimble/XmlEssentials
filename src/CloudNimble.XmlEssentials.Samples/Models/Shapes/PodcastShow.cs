using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// The start of a podcast feed: an RSS document with one element from the iTunes namespace, whose prefix the
    /// type declares.
    /// </summary>
    [XmlRoot("rss")]
    [XmlNamespaceDeclaration("itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd")]
    public sealed class PodcastShow
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the author, written as <c>itunes:author</c>.
        /// </summary>
        [XmlElement("author", "http://www.itunes.com/dtds/podcast-1.0.dtd")]
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets the title, written as <c>title</c> in no namespace.
        /// </summary>
        [XmlElement("title")]
        public string? Title { get; set; }

        #endregion

    }

}
