using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// The author of an <see cref="Article"/>: an attribute and the element's text.
    /// </summary>
    public sealed class ArticleAuthor
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the author's email address, written as the <c>email</c> attribute.
        /// </summary>
        [XmlAttribute("email")]
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the author's name, written as the text of the element.
        /// </summary>
        [XmlText]
        public string? Name { get; set; }

        #endregion

    }

}
