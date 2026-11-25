namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A declared derived type of <see cref="FallbackProduct"/>.
    /// </summary>
    public sealed class FallbackBook : FallbackProduct
    {

        #region Public Properties

        /// <summary>
        /// Gets the author.
        /// </summary>
        public string? Author { get; set; }

        #endregion

    }

}
