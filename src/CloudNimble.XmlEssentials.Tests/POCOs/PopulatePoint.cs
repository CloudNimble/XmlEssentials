namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A value type held by a member that may be populated.
    /// </summary>
    public struct PopulatePoint
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value the documents in the tests leave out.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Gets or sets a value the documents in the tests give.
        /// </summary>
        public int Y { get; set; }

        #endregion

    }

}
