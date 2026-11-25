namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Two members that may hold the same record instance.
    /// </summary>
    public sealed class RecordPair
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the first member.
        /// </summary>
        public PersonRecord? First { get; set; }

        /// <summary>
        /// Gets or sets the second member.
        /// </summary>
        public PersonRecord? Second { get; set; }

        #endregion

    }

}
