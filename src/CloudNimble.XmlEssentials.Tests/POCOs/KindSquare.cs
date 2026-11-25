namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A shape that <see cref="KindShape"/> and <see cref="IKindShape"/> declare as a derived type.
    /// </summary>
    public sealed class KindSquare : KindShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        public double Side { get; set; }

        #endregion

    }

}
