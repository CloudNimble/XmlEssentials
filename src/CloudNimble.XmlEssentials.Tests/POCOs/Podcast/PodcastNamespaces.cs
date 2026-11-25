namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// The namespaces a podcast feed takes its elements from, besides RSS itself, which has none.
    /// </summary>
    public static class PodcastNamespaces
    {

        #region Fields

        /// <summary>
        /// The Atom syndication format, for the links of a channel.
        /// </summary>
        public const string Atom = "http://www.w3.org/2005/Atom";

        /// <summary>
        /// The RSS content module, for the full text of an episode.
        /// </summary>
        public const string Content = "http://purl.org/rss/1.0/modules/content/";

        /// <summary>
        /// The iTunes podcast extension.
        /// </summary>
        public const string Itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";

        /// <summary>
        /// The Podcast Index namespace.
        /// </summary>
        public const string Podcast = "https://podcastindex.org/namespace/1.0";

        #endregion

    }

}
