namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Two members of the same polymorphic type, so each derived element name could belong to either.
    /// </summary>
    public sealed class TwinShelf
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the first item.
        /// </summary>
        public Product? First { get; set; }

        /// <summary>
        /// Gets or sets the second item.
        /// </summary>
        public Product? Second { get; set; }

        #endregion

    }

}
