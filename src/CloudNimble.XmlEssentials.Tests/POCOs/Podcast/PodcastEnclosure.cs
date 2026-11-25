using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// The <c>enclosure</c> of an episode: its audio file.
    /// </summary>
    /// <remarks>
    /// The members are in alphabetical order. <see cref="XmlPropertyOrderAttribute"/> gives each the place it
    /// has in the feed.
    /// </remarks>
    public sealed class PodcastEnclosure
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the size of the file, in bytes.
        /// </summary>
        [XmlAttribute("length")]
        [XmlPropertyOrder(1)]
        public long Length { get; set; }

        /// <summary>
        /// Gets or sets the media type of the file.
        /// </summary>
        [XmlAttribute("type")]
        [XmlPropertyOrder(2)]
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the address of the file.
        /// </summary>
        [XmlAttribute("url")]
        [XmlPropertyOrder(0)]
        public string? Url { get; set; }

        #endregion

    }

}
