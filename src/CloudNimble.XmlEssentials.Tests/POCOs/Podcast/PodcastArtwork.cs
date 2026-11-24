using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// An <c>itunes:image</c>: the artwork of a show or of an episode.
    /// </summary>
    /// <remarks>
    /// The members are in alphabetical order. <see cref="XmlPropertyOrderAttribute"/> gives each the place it
    /// has in the feed.
    /// </remarks>
    public sealed class PodcastArtwork
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the address of the artwork.
        /// </summary>
        [XmlAttribute("href")]
        [XmlPropertyOrder(0)]
        public string? Href { get; set; }

        #endregion

    }

}
