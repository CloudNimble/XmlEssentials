namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that holds an object whose name only its constructor can set.
    /// </summary>
    public sealed class PopulateConstructed
    {

        #region Public Properties

        /// <summary>
        /// Gets the object, which was created with a name.
        /// </summary>
        public GetterOnlyCtor Child { get; } = new("kept");

        #endregion

    }

}
