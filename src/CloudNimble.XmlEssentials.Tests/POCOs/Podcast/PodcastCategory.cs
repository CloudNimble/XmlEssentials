using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// An <c>itunes:category</c>, which can hold subcategories.
    /// </summary>
    /// <remarks>
    /// The members are in alphabetical order. <see cref="XmlPropertyOrderAttribute"/> gives each the place it
    /// has in the feed.
    /// </remarks>
    public sealed class PodcastCategory
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the subcategories.
        /// </summary>
        [XmlElement("category", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(1)]
        public List<PodcastCategory> Subcategories { get; set; } = [];

        /// <summary>
        /// Gets or sets the name of the category.
        /// </summary>
        [XmlAttribute("text")]
        [XmlPropertyOrder(0)]
        public string? Text { get; set; }

        #endregion

    }

}
