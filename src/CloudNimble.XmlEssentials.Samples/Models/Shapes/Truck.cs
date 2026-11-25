namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A truck.
    /// </summary>
    public sealed class Truck : Vehicle
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the load the truck can carry, in kilograms.
        /// </summary>
        public int Payload { get; set; }

        #endregion

    }

}
