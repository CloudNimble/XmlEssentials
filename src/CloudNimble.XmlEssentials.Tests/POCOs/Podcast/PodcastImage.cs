using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// The RSS <c>image</c> of a channel.
    /// </summary>
    /// <remarks>
    /// The members are in alphabetical order. <see cref="XmlPropertyOrderAttribute"/> gives each the place it
    /// has in the feed.
    /// </remarks>
    public sealed class PodcastImage
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the page the picture links to, which the feed writes as a CDATA section.
        /// </summary>
        [XmlElement("link")]
        [XmlConverter<PodcastCDataConverter>]
        [XmlPropertyOrder(2)]
        public string? Link { get; set; }

        /// <summary>
        /// Gets or sets the title of the picture.
        /// </summary>
        [XmlElement("title")]
        [XmlPropertyOrder(1)]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the address of the picture.
        /// </summary>
        [XmlElement("url")]
        [XmlPropertyOrder(0)]
        public string? Url { get; set; }

        #endregion

    }

}
