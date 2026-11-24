using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// An <c>atom:link</c> of a channel: the address of the feed itself, or of its hub.
    /// </summary>
    /// <remarks>
    /// The members are in alphabetical order. <see cref="XmlPropertyOrderAttribute"/> gives each the place it
    /// has in the feed.
    /// </remarks>
    public sealed class PodcastAtomLink
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        [XmlAttribute("href")]
        [XmlPropertyOrder(0)]
        public string? Href { get; set; }

        /// <summary>
        /// Gets or sets the relation: <c>self</c> or <c>hub</c>.
        /// </summary>
        [XmlAttribute("rel")]
        [XmlPropertyOrder(1)]
        public string? Rel { get; set; }

        /// <summary>
        /// Gets or sets the media type, or <see langword="null"/> when the link does not state one.
        /// </summary>
        [XmlAttribute("type")]
        [XmlPropertyOrder(2)]
        public string? Type { get; set; }

        #endregion

    }

}
