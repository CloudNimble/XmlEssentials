namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds one value of a declared type that may be abstract.
    /// </summary>
    /// <typeparam name="T">The declared type.</typeparam>
    public sealed class PolyItem<T>
        where T : class
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets what is held.
        /// </summary>
        public T? Item { get; set; }

        #endregion

    }

}
