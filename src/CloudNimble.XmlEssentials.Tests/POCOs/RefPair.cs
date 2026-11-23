namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds two members that may be the same object, for reference handling of members.
    /// </summary>
    public sealed class RefPair
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the first member.
        /// </summary>
        public RefLeaf? First { get; set; }

        /// <summary>
        /// Gets or sets the second member.
        /// </summary>
        public RefLeaf? Second { get; set; }

        #endregion

    }

}
