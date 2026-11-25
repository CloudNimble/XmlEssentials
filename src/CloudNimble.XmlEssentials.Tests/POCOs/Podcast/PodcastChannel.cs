using System;
using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// The <c>channel</c> of a podcast feed: the show, and its episodes.
    /// </summary>
    /// <remarks>
    /// The members are in alphabetical order. <see cref="XmlPropertyOrderAttribute"/> gives each the place it
    /// has in the feed.
    /// </remarks>
    public sealed class PodcastChannel
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the iTunes artwork.
        /// </summary>
        [XmlElement("image", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(10)]
        public PodcastArtwork? Artwork { get; set; }

        /// <summary>
        /// Gets or sets the Atom links of the feed: its own address, and its hub.
        /// </summary>
        [XmlElement("link", PodcastNamespaces.Atom)]
        [XmlPropertyOrder(0)]
        public List<PodcastAtomLink> AtomLinks { get; set; } = [];

        /// <summary>
        /// Gets or sets the author.
        /// </summary>
        [XmlElement("author", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(12)]
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets the categories.
        /// </summary>
        [XmlElement("category", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(17)]
        public List<PodcastCategory> Categories { get; set; } = [];

        /// <summary>
        /// Gets or sets the copyright notice, which the feed writes as a CDATA section.
        /// </summary>
        [XmlElement("copyright")]
        [XmlConverter<PodcastCDataConverter>]
        [XmlPropertyOrder(6)]
        public string? Copyright { get; set; }

        /// <summary>
        /// Gets or sets the description of the show.
        /// </summary>
        [XmlElement("description")]
        [XmlPropertyOrder(13)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the episodes, newest first.
        /// </summary>
        [XmlElement("item")]
        [XmlPropertyOrder(21)]
        public List<PodcastEpisode> Episodes { get; set; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether the show is explicit.
        /// </summary>
        [XmlElement("explicit", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(15)]
        public bool Explicit { get; set; }

        /// <summary>
        /// Gets or sets the program that wrote the feed.
        /// </summary>
        [XmlElement("generator")]
        [XmlPropertyOrder(4)]
        public string? Generator { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the show.
        /// </summary>
        [XmlElement("guid", PodcastNamespaces.Podcast)]
        [XmlPropertyOrder(2)]
        public Guid Guid { get; set; }

        /// <summary>
        /// Gets or sets the RSS image.
        /// </summary>
        [XmlElement("image")]
        [XmlPropertyOrder(9)]
        public PodcastImage? Image { get; set; }

        /// <summary>
        /// Gets or sets the language of the show, which the feed writes as a CDATA section.
        /// </summary>
        [XmlElement("language")]
        [XmlConverter<PodcastCDataConverter>]
        [XmlPropertyOrder(5)]
        public string? Language { get; set; }

        /// <summary>
        /// Gets or sets the time the feed was last built.
        /// </summary>
        [XmlElement("lastBuildDate")]
        [XmlConverter<PodcastDateConverter>]
        [XmlPropertyOrder(3)]
        public DateTimeOffset LastBuildDate { get; set; }

        /// <summary>
        /// Gets or sets the home page of the show.
        /// </summary>
        [XmlElement("link")]
        [XmlPropertyOrder(14)]
        public string? Link { get; set; }

        /// <summary>
        /// Gets or sets whether the feed may be imported by another host: <c>yes</c> or <c>no</c>.
        /// </summary>
        [XmlElement("locked", PodcastNamespaces.Podcast)]
        [XmlPropertyOrder(19)]
        public string? Locked { get; set; }

        /// <summary>
        /// Gets or sets the managing editor.
        /// </summary>
        [XmlElement("managingEditor")]
        [XmlPropertyOrder(7)]
        public string? ManagingEditor { get; set; }

        /// <summary>
        /// Gets or sets the medium, such as <c>podcast</c>.
        /// </summary>
        [XmlElement("medium", PodcastNamespaces.Podcast)]
        [XmlPropertyOrder(20)]
        public string? Medium { get; set; }

        /// <summary>
        /// Gets or sets the address the feed has moved to.
        /// </summary>
        [XmlElement("new-feed-url", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(18)]
        public string? NewFeedUrl { get; set; }

        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        [XmlElement("owner", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(11)]
        public PodcastOwner? Owner { get; set; }

        /// <summary>
        /// Gets or sets the iTunes summary, which the feed writes as a CDATA section.
        /// </summary>
        [XmlElement("summary", PodcastNamespaces.Itunes)]
        [XmlConverter<PodcastCDataConverter>]
        [XmlPropertyOrder(8)]
        public string? Summary { get; set; }

        /// <summary>
        /// Gets or sets the title of the show, which the feed writes as a CDATA section.
        /// </summary>
        [XmlElement("title")]
        [XmlConverter<PodcastCDataConverter>]
        [XmlPropertyOrder(1)]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the type of show: <c>episodic</c> or <c>serial</c>.
        /// </summary>
        [XmlElement("type", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(16)]
        public string? Type { get; set; }

        #endregion

    }

}
