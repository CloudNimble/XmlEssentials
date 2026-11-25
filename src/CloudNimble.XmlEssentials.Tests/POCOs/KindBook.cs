namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A book whose kind discriminator is <c>book</c>.
    /// </summary>
    public sealed class KindBook : KindProduct
    {

        #region Public Properties

        /// <summary>
        /// Gets the author.
        /// </summary>
        public string? Author { get; set; }

        #endregion

    }

}
