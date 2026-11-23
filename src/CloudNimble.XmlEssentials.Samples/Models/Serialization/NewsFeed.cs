using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A feed whose entries are repeated elements with a name of their own.
    /// </summary>
    public sealed class NewsFeed
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the title of the feed.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the headlines, written as repeated <c>Entry</c> elements.
        /// </summary>
        [XmlElement("Entry")]
        public List<string> Entries { get; set; } = [];

        #endregion

    }

}
