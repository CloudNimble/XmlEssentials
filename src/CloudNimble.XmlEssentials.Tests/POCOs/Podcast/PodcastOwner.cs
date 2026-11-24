using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// The <c>itunes:owner</c> of a show.
    /// </summary>
    /// <remarks>
    /// The members are in alphabetical order. <see cref="XmlPropertyOrderAttribute"/> gives each the place it
    /// has in the feed.
    /// </remarks>
    public sealed class PodcastOwner
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the owner's name.
        /// </summary>
        [XmlElement("name", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(0)]
        public string? Name { get; set; }

        #endregion

    }

}
