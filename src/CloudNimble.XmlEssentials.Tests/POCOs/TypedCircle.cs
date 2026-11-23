namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A shape that <see cref="TypedShape"/> and <see cref="ITypedShape"/> declare as a derived type.
    /// </summary>
    public sealed class TypedCircle : TypedShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        public double Radius { get; set; }

        #endregion

    }

}
