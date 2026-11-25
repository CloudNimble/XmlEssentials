namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A shape that <see cref="LooseShape"/> declares as a derived type.
    /// </summary>
    public sealed class LooseCircle : LooseShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        public double Radius { get; set; }

        #endregion

    }

}
