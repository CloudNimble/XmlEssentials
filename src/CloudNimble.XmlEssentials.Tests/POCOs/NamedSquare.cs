namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A shape that <see cref="NamedShape"/> and <see cref="INamedShape"/> declare as a derived type.
    /// </summary>
    public sealed class NamedSquare : NamedShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        public double Side { get; set; }

        #endregion

    }

}
