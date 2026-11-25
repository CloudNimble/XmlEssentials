namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A member named like the element one of another member's derived types is written under.
    /// </summary>
    public sealed class BookNamedShelf
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a label, written as <c>Book</c>.
        /// </summary>
        public string? Book { get; set; }

        /// <summary>
        /// Gets or sets the item, which is written as <c>Book</c> when it is a <see cref="POCOs.Book"/>.
        /// </summary>
        public Product? Item { get; set; }

        #endregion

    }

}
