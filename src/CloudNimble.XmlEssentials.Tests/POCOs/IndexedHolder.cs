namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with an indexer, which is not serialized, and a name, which is.
    /// </summary>
    public sealed class IndexedHolder
    {

        #region Public Properties

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets an indexer that is not a serialized member.
        /// </summary>
        /// <param name="index">The index.</param>
        public int this[int index]
        {
            get => 0;
            set { }
        }

        #endregion

    }

}
