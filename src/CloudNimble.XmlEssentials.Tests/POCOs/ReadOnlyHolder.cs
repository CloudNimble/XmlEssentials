namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a get-only property.
    /// </summary>
    public sealed class ReadOnlyHolder
    {

        #region Public Properties

        /// <summary>
        /// Gets a computed value.
        /// </summary>
        public int N => 7;

        /// <summary>
        /// Gets a writable value.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
