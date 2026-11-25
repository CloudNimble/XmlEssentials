namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// What the writer keeps about an open element until its end tag is written.
    /// </summary>
    /// <remarks>
    /// The state of the element that is being written (whether its start tag is open, whether it holds content,
    /// how many attributes it has) is held by the writer itself, because only the innermost element has any.
    /// A frame holds the name the end tag needs, and what the writer goes back to when the element ends. It
    /// holds no references, so writing one costs the collector nothing.
    /// </remarks>
    internal struct XmlWriterFrame
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets where the local name is in the writer's name buffer.
        /// </summary>
        public XmlWriterName Local { get; set; }

        /// <summary>
        /// Gets or sets the length of the writer's name buffer when the element was started. The buffer goes
        /// back to it when the element ends.
        /// </summary>
        public int NamesMark { get; set; }

        /// <summary>
        /// Gets or sets whether the element that holds this one had character data when this one was started,
        /// which turns off indentation inside that element.
        /// </summary>
        public bool ParentHasText { get; set; }

        /// <summary>
        /// Gets or sets where the prefix is in the writer's name buffer; empty when the name has no prefix.
        /// </summary>
        public XmlWriterName Prefix { get; set; }

        #endregion

    }

}
