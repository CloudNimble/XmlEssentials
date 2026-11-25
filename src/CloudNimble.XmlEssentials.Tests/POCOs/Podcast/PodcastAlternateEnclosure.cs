using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// A <c>podcast:alternateEnclosure</c>: another form of an episode, such as a video.
    /// </summary>
    /// <remarks>
    /// The members are in alphabetical order. <see cref="XmlPropertyOrderAttribute"/> gives each the place it
    /// has in the feed.
    /// </remarks>
    public sealed class PodcastAlternateEnclosure
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the height of the video in pixels, or <see langword="null"/> when none is stated.
        /// </summary>
        [XmlAttribute("height")]
        [XmlPropertyOrder(2)]
        public int? Height { get; set; }

        /// <summary>
        /// Gets or sets where this form of the episode can be fetched from.
        /// </summary>
        [XmlElement("source", PodcastNamespaces.Podcast)]
        [XmlPropertyOrder(3)]
        public PodcastSource? Source { get; set; }

        /// <summary>
        /// Gets or sets the title, or <see langword="null"/> when none is stated.
        /// </summary>
        [XmlAttribute("title")]
        [XmlPropertyOrder(1)]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the media type.
        /// </summary>
        [XmlAttribute("type")]
        [XmlPropertyOrder(0)]
        public string? Type { get; set; }

        #endregion

    }

}
