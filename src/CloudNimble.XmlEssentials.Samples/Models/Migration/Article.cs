using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// A blog article that uses the shapes XML has and JSON does not: a root name, a namespace, an attribute, and a wrapped list.
    /// </summary>
    [XmlRoot("article", Namespace = "urn:contoso:blog")]
    public sealed class Article
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the author, written as an element that has an attribute and text.
        /// </summary>
        [XmlPropertyName("author")]
        public ArticleAuthor? Author { get; set; }

        /// <summary>
        /// Gets or sets the article's number, written as the <c>id</c> attribute.
        /// </summary>
        [XmlAttribute("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the tags, written as <c>tag</c> elements inside a <c>tags</c> element.
        /// </summary>
        [XmlArray("tags")]
        [XmlArrayItem("tag")]
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// Gets or sets the headline, written as the <c>headline</c> element.
        /// </summary>
        [XmlPropertyName("headline")]
        public string? Title { get; set; }

        #endregion

    }

}
