namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An object held by a member, by an item of a collection in a wrapper, and by an item of one with none.
    /// </summary>
    public sealed class ResumeAddress
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the city.
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Gets or sets the postal code.
        /// </summary>
        public string? Zip { get; set; }

        #endregion

    }

}
