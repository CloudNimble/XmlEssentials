namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An object held by a member that may be populated.
    /// </summary>
    public sealed class PopulateChild
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value the documents in the tests leave out.
        /// </summary>
        public int A { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value the documents in the tests give.
        /// </summary>
        public int B { get; set; }

        #endregion

    }

}
