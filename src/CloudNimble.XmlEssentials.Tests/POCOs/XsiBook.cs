namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A book whose <c>xsi:type</c> is <c>Book</c>.
    /// </summary>
    public sealed class XsiBook : XsiProduct
    {

        #region Public Properties

        /// <summary>
        /// Gets the author.
        /// </summary>
        public string? Author { get; set; }

        #endregion

    }

}
