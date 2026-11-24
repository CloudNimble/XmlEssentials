namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with one member annotated non-nullable and one annotated nullable.
    /// </summary>
    public sealed class Annotated
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value that may be null.
        /// </summary>
        public string? Maybe { get; set; }

        /// <summary>
        /// Gets or sets a value annotated as never null.
        /// </summary>
        public string Name { get; set; } = "n";

        #endregion

    }

}
