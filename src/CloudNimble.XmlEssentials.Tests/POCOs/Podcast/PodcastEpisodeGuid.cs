using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// The <c>guid</c> of an episode: an identifier, and whether it is also the episode's address.
    /// </summary>
    /// <remarks>
    /// The members are in alphabetical order. <see cref="XmlPropertyOrderAttribute"/> gives each the place it
    /// has in the feed.
    /// </remarks>
    public sealed class PodcastEpisodeGuid
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value indicating whether the identifier is a permanent link to the episode.
        /// </summary>
        [XmlAttribute("isPermaLink")]
        [XmlPropertyOrder(0)]
        public bool IsPermaLink { get; set; }

        /// <summary>
        /// Gets or sets the identifier, which is not a GUID with every host.
        /// </summary>
        [XmlText]
        [XmlPropertyOrder(1)]
        public string? Value { get; set; }

        #endregion

    }

}
