namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Represents a run of text in mixed content. This class cannot be inherited.
    /// </summary>
    public sealed class XmlMixedText : XmlMixedContentItem
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the text of the run.
        /// </summary>
        /// <value>
        /// The text of the run. The default is an empty string.
        /// </value>
        public string Text { get; set; } = "";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlMixedText"/> class that has empty text.
        /// </summary>
        public XmlMixedText()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlMixedText"/> class with the specified text.
        /// </summary>
        /// <param name="text">The text of the run.</param>
        public XmlMixedText(string text)
        {
            Text = text;
        }

        #endregion

    }

}
