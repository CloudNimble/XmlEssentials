using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// A <c>podcast:transcript</c> of an episode.
    /// </summary>
    /// <remarks>
    /// The members are in alphabetical order. <see cref="XmlPropertyOrderAttribute"/> gives each the place it
    /// has in the feed.
    /// </remarks>
    public sealed class PodcastTranscript
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the relation, such as <c>captions</c>, or <see langword="null"/> when none is stated.
        /// </summary>
        [XmlAttribute("rel")]
        [XmlPropertyOrder(2)]
        public string? Rel { get; set; }

        /// <summary>
        /// Gets or sets the media type of the transcript.
        /// </summary>
        [XmlAttribute("type")]
        [XmlPropertyOrder(1)]
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the address of the transcript.
        /// </summary>
        [XmlAttribute("url")]
        [XmlPropertyOrder(0)]
        public string? Url { get; set; }

        #endregion

    }

}
