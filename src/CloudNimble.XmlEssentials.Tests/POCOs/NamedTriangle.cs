namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A shape that <see cref="NamedShape"/> does not declare as a derived type.
    /// </summary>
    public sealed class NamedTriangle : NamedShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        public double Height { get; set; }

        #endregion

    }

}
