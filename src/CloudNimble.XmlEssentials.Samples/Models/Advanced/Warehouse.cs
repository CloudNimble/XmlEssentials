namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// A warehouse and where it is.
    /// </summary>
    public sealed class Warehouse
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the position of the warehouse.
        /// </summary>
        public GeoPoint Location { get; set; }

        /// <summary>
        /// Gets or sets the name of the warehouse.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
