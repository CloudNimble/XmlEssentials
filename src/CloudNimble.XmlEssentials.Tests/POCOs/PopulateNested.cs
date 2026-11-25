namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that holds an object that itself holds values to populate.
    /// </summary>
    public sealed class PopulateNested
    {

        #region Public Properties

        /// <summary>
        /// Gets the inner object.
        /// </summary>
        public PopulateInner Inner { get; } = new();

        #endregion

    }

}
