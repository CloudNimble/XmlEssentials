namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// The middle of the hierarchy: a derived type that has a derived type of its own.
    /// </summary>
    public class TierBook : TierProduct
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the author.
        /// </summary>
        public string? Author { get; set; }

        #endregion

    }

}
