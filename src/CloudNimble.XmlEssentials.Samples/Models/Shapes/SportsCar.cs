namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A kind of car that <see cref="Vehicle"/> does not declare as a derived type.
    /// </summary>
    public sealed class SportsCar : Car
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the top speed, in kilometers per hour.
        /// </summary>
        public int TopSpeed { get; set; }

        #endregion

    }

}
