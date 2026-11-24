using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose text is annotated non-nullable.
    /// </summary>
    public sealed class AnnotatedText
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the text of the element.
        /// </summary>
        [XmlText]
        public string Body { get; set; } = "b";

        #endregion

    }

}
