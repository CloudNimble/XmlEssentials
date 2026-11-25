namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// A <see cref="Product"/> that is a book.
    /// </summary>
    public sealed class Book : Product
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the author.
        /// </summary>
        public string? Author { get; set; }

        #endregion

    }

}
