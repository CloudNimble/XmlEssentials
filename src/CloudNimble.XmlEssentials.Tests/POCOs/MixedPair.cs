namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Two members of different types, for a reference that points at the wrong one.
    /// </summary>
    public sealed class MixedPair
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the first member.
        /// </summary>
        public Node? First { get; set; }

        /// <summary>
        /// Gets or sets the second member.
        /// </summary>
        public Person? Second { get; set; }

        #endregion

    }

}
