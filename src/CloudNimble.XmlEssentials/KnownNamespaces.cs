using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Provides the namespace declarations of well-known XML formats, each as the prefixes the format is
    /// customarily written with and the namespace URIs they are bound to.
    /// </summary>
    /// <example>
    /// <code>
    /// var options = new XmlSerializerOptions { Namespaces = KnownNamespaces.Podcast };
    /// string xml = XmlSerializer.Serialize(feed, options);
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>
    /// Assign one of these to <see cref="XmlSerializerOptions.Namespaces"/> to have the serializer declare the
    /// namespaces on the document element and write the customary prefixes. Every declaration of a set is
    /// written, whether or not the document uses it. The sets are read-only, and their declarations are in
    /// alphabetical order of prefix.
    /// </para>
    /// <para>
    /// A set holds the namespaces a format adds to its own. A format whose own elements have no namespace, such
    /// as RSS, or are customarily written in the default namespace, such as a sitemap, is not in its set. To use
    /// the prefixes of a particular document instead, see
    /// <see cref="XmlDocument.DiscoverNamespaces(ReadOnlySpan{byte}, XmlDocumentOptions)"/>.
    /// </para>
    /// </remarks>
    public static class KnownNamespaces
    {

        #region Properties

        /// <summary>
        /// Gets the namespace of an OData version 4 metadata document (CSDL in XML), which is customarily written
        /// with the <c>edmx</c> prefix.
        /// </summary>
        /// <value>
        /// <c>edmx</c>. The Entity Data Model namespace of the <c>Schema</c> element is not in the set: it is
        /// customarily the default namespace of that element.
        /// </value>
        public static IReadOnlyDictionary<string, string> Edmx { get; } = Create(
            ("edmx", "http://docs.oasis-open.org/odata/ns/edmx"));

        /// <summary>
        /// Gets the namespaces of a podcast feed: the iTunes and Podcasting 2.0 extensions to RSS, and the modules
        /// that podcast hosts add.
        /// </summary>
        /// <value>
        /// <c>atom</c>, <c>content</c>, <c>dc</c>, <c>googleplay</c>, <c>itunes</c>, <c>media</c>,
        /// <c>podcast</c> (Podcasting 2.0, the Podcast Index namespace), <c>psc</c> (Podlove Simple Chapters)
        /// and <c>sy</c>.
        /// </value>
        public static IReadOnlyDictionary<string, string> Podcast { get; } = Create(
            ("atom", "http://www.w3.org/2005/Atom"),
            ("content", "http://purl.org/rss/1.0/modules/content/"),
            ("dc", "http://purl.org/dc/elements/1.1/"),
            ("googleplay", "http://www.google.com/schemas/play-podcasts/1.0"),
            ("itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd"),
            ("media", "http://search.yahoo.com/mrss/"),
            ("podcast", "https://podcastindex.org/namespace/1.0"),
            ("psc", "http://podlove.org/simple-chapters"),
            ("sy", "http://purl.org/rss/1.0/modules/syndication/"));

        /// <summary>
        /// Gets the namespaces of the modules that RSS 2.0 feeds commonly use.
        /// </summary>
        /// <value>
        /// <c>atom</c>, <c>content</c>, <c>dc</c>, <c>georss</c>, <c>media</c>, <c>slash</c>, <c>sy</c> and
        /// <c>wfw</c>.
        /// </value>
        public static IReadOnlyDictionary<string, string> Rss { get; } = Create(
            ("atom", "http://www.w3.org/2005/Atom"),
            ("content", "http://purl.org/rss/1.0/modules/content/"),
            ("dc", "http://purl.org/dc/elements/1.1/"),
            ("georss", "http://www.georss.org/georss"),
            ("media", "http://search.yahoo.com/mrss/"),
            ("slash", "http://purl.org/rss/1.0/modules/slash/"),
            ("sy", "http://purl.org/rss/1.0/modules/syndication/"),
            ("wfw", "http://wellformedweb.org/CommentAPI/"));

        /// <summary>
        /// Gets the namespaces of the extensions to a sitemap.
        /// </summary>
        /// <value>
        /// <c>image</c>, <c>news</c>, <c>video</c> and <c>xhtml</c>.
        /// </value>
        public static IReadOnlyDictionary<string, string> Sitemap { get; } = Create(
            ("image", "http://www.google.com/schemas/sitemap-image/1.1"),
            ("news", "http://www.google.com/schemas/sitemap-news/0.9"),
            ("video", "http://www.google.com/schemas/sitemap-video/1.1"),
            ("xhtml", "http://www.w3.org/1999/xhtml"));

        /// <summary>
        /// Gets the namespaces of a SOAP 1.1 message.
        /// </summary>
        /// <value>
        /// <c>soap</c>, <c>xsd</c> and <c>xsi</c>.
        /// </value>
        /// <remarks>
        /// The <c>soap</c> prefix is the one .NET and most toolkits write. The specification's own examples use
        /// <c>SOAP-ENV</c>.
        /// </remarks>
        public static IReadOnlyDictionary<string, string> Soap11 { get; } = Create(
            ("soap", "http://schemas.xmlsoap.org/soap/envelope/"),
            ("xsd", XmlNamespaces.Xsd),
            ("xsi", XmlNamespaces.Xsi));

        /// <summary>
        /// Gets the namespaces of a SOAP 1.2 message.
        /// </summary>
        /// <value>
        /// <c>soap</c>, <c>xsd</c> and <c>xsi</c>.
        /// </value>
        /// <remarks>
        /// The <c>soap</c> prefix is the one .NET and most toolkits write. The specification's own examples use
        /// <c>env</c>.
        /// </remarks>
        public static IReadOnlyDictionary<string, string> Soap12 { get; } = Create(
            ("soap", "http://www.w3.org/2003/05/soap-envelope"),
            ("xsd", XmlNamespaces.Xsd),
            ("xsi", XmlNamespaces.Xsi));

        /// <summary>
        /// Gets the namespaces of an SVG picture.
        /// </summary>
        /// <value>
        /// <c>svg</c> and <c>xlink</c>.
        /// </value>
        public static IReadOnlyDictionary<string, string> Svg { get; } = Create(
            ("svg", "http://www.w3.org/2000/svg"),
            ("xlink", "http://www.w3.org/1999/xlink"));

        /// <summary>
        /// Gets the namespaces of XML Schema: the schema language, and the attributes an instance document takes
        /// from it.
        /// </summary>
        /// <value>
        /// <c>xsd</c> and <c>xsi</c>.
        /// </value>
        public static IReadOnlyDictionary<string, string> XmlSchema { get; } = Create(
            ("xsd", XmlNamespaces.Xsd),
            ("xsi", XmlNamespaces.Xsi));

        #endregion

        #region Private Methods

        private static ReadOnlyDictionary<string, string> Create(params ReadOnlySpan<(string Prefix, string NamespaceUri)> declarations)
        {
            var set = new OrderedDictionary<string, string>(declarations.Length, StringComparer.Ordinal);
            foreach (var (prefix, namespaceUri) in declarations)
            {
                set.Add(prefix, namespaceUri);
            }

            return new ReadOnlyDictionary<string, string>(set);
        }

        #endregion

    }

}
