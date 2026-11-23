namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a required property.
    /// </summary>
    public sealed class RequiredCatalog
    {

        #region Public Properties

        /// <summary>
        /// Gets the required title.
        /// </summary>
        public required string Title { get; set; }

        #endregion

    }

}
