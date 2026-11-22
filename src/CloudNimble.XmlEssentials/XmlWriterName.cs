namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// A slice into the writer's name store.
    /// </summary>
    internal struct XmlWriterName
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the UTF-8 byte length.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Gets or sets the start offset in the writer's name store.
        /// </summary>
        public int Start { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a slice.
        /// </summary>
        /// <param name="start">The start offset.</param>
        /// <param name="length">The byte length.</param>
        /// <returns>The slice.</returns>
        public static XmlWriterName From(int start, int length)
        {
            return new XmlWriterName { Start = start, Length = length };
        }

        #endregion

    }

}
