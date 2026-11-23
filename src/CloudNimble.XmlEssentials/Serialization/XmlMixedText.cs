namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// A text run in mixed content.
    /// </summary>
    public sealed class XmlMixedText : XmlMixedContentItem
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string Text { get; set; } = "";

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an empty text run.
        /// </summary>
        public XmlMixedText()
        {
        }

        /// <summary>
        /// Creates a text run.
        /// </summary>
        /// <param name="text">The text.</param>
        public XmlMixedText(string text)
        {
            Text = text;
        }

        #endregion

    }

}
