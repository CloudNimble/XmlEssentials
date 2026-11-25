namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose properties are init-only.
    /// </summary>
    public sealed class InitWidget
    {

        #region Public Properties

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// Gets the label.
        /// </summary>
        public string Label { get; init; } = "";

        #endregion

    }

}
