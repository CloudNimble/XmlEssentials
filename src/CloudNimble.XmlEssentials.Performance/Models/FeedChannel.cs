using System.Collections.Generic;
using Bcl = System.Xml.Serialization;
using Xe = CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// The channel of a <see cref="Feed"/>.
    /// </summary>
    public sealed class FeedChannel
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the Atom link to the feed itself.
        /// </summary>
        [Bcl.XmlElement("link", Namespace = Feed.Atom)]
        [Xe.XmlElement("link", Feed.Atom)]
        public FeedLink? AtomLink { get; set; }

        /// <summary>
        /// Gets or sets the iTunes author.
        /// </summary>
        [Bcl.XmlElement("author", Namespace = Feed.ITunes)]
        [Xe.XmlElement("author", Feed.ITunes)]
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets the episodes.
        /// </summary>
        [Bcl.XmlElement("item")]
        [Xe.XmlElement("item")]
        public List<FeedItem>? Items { get; set; }

        /// <summary>
        /// Gets or sets the link to the site.
        /// </summary>
        [Bcl.XmlElement("link")]
        [Xe.XmlElement("link")]
        public string? Link { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [Bcl.XmlElement("title")]
        [Xe.XmlElement("title")]
        public string? Title { get; set; }

        #endregion

    }

}
