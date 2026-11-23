namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A kind of book that <see cref="Product"/> does not declare as a derived type.
    /// </summary>
    public sealed class Ebook : Book
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the file format.
        /// </summary>
        public string? Format { get; set; }

        #endregion

    }

}
