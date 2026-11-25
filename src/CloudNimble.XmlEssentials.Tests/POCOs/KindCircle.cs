namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A shape that <see cref="KindShape"/> and <see cref="IKindShape"/> declare as a derived type.
    /// </summary>
    public sealed class KindCircle : KindShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        public double Radius { get; set; }

        #endregion

    }

}
