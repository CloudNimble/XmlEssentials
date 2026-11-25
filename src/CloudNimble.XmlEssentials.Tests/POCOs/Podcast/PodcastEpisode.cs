using System;
using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// An <c>item</c> of a podcast feed: one episode.
    /// </summary>
    /// <remarks>
    /// The members are in alphabetical order. <see cref="XmlPropertyOrderAttribute"/> gives each the place it
    /// has in the feed.
    /// </remarks>
    public sealed class PodcastEpisode
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the other forms of the episode.
        /// </summary>
        [XmlElement("alternateEnclosure", PodcastNamespaces.Podcast)]
        [XmlPropertyOrder(17)]
        public List<PodcastAlternateEnclosure> AlternateEnclosures { get; set; } = [];

        /// <summary>
        /// Gets or sets the artwork.
        /// </summary>
        [XmlElement("image", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(6)]
        public PodcastArtwork? Artwork { get; set; }

        /// <summary>
        /// Gets or sets the full show notes, as HTML, which the feed writes as a CDATA section.
        /// </summary>
        [XmlElement("encoded", PodcastNamespaces.Content)]
        [XmlConverter<PodcastCDataConverter>]
        [XmlPropertyOrder(3)]
        public string? Content { get; set; }

        /// <summary>
        /// Gets or sets the description, as HTML, which the feed writes as a CDATA section.
        /// </summary>
        [XmlElement("description")]
        [XmlConverter<PodcastCDataConverter>]
        [XmlPropertyOrder(2)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the running time, as <c>mm:ss</c> or <c>h:mm:ss</c>.
        /// </summary>
        [XmlElement("duration", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(9)]
        public string? Duration { get; set; }

        /// <summary>
        /// Gets or sets the audio file.
        /// </summary>
        [XmlElement("enclosure")]
        [XmlPropertyOrder(8)]
        public PodcastEnclosure? Enclosure { get; set; }

        /// <summary>
        /// Gets or sets the episode number.
        /// </summary>
        [XmlElement("episode", PodcastNamespaces.Podcast)]
        [XmlPropertyOrder(14)]
        public int Episode { get; set; }

        /// <summary>
        /// Gets or sets the type of episode, such as <c>full</c>.
        /// </summary>
        [XmlElement("episodeType", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(11)]
        public string? EpisodeType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the episode is explicit.
        /// </summary>
        [XmlElement("explicit", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(10)]
        public bool Explicit { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [XmlElement("guid")]
        [XmlPropertyOrder(5)]
        public PodcastEpisodeGuid? Guid { get; set; }

        /// <summary>
        /// Gets or sets the episode number, for iTunes.
        /// </summary>
        [XmlElement("episode", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(13)]
        public int ItunesEpisode { get; set; }

        /// <summary>
        /// Gets or sets the season number, for iTunes.
        /// </summary>
        [XmlElement("season", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(12)]
        public int ItunesSeason { get; set; }

        /// <summary>
        /// Gets or sets the title iTunes shows, which leaves out the season and episode numbers.
        /// </summary>
        [XmlElement("title", PodcastNamespaces.Itunes)]
        [XmlPropertyOrder(1)]
        public string? ItunesTitle { get; set; }

        /// <summary>
        /// Gets or sets the page of the episode, which the feed writes as a CDATA section.
        /// </summary>
        [XmlElement("link")]
        [XmlConverter<PodcastCDataConverter>]
        [XmlPropertyOrder(4)]
        public string? Link { get; set; }

        /// <summary>
        /// Gets or sets the time the episode was published.
        /// </summary>
        [XmlElement("pubDate")]
        [XmlConverter<PodcastDateConverter>]
        [XmlPropertyOrder(7)]
        public DateTimeOffset PublishedOn { get; set; }

        /// <summary>
        /// Gets or sets the season number.
        /// </summary>
        [XmlElement("season", PodcastNamespaces.Podcast)]
        [XmlPropertyOrder(15)]
        public int Season { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [XmlElement("title")]
        [XmlPropertyOrder(0)]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the transcripts.
        /// </summary>
        [XmlElement("transcript", PodcastNamespaces.Podcast)]
        [XmlPropertyOrder(16)]
        public List<PodcastTranscript> Transcripts { get; set; } = [];

        #endregion

    }

}
