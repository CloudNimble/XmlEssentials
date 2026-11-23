namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A shipment with no XML attributes of its own. The samples put it in a namespace through the serializer options.
    /// </summary>
    public sealed class Shipment
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the shipment number.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the carrier.
        /// </summary>
        public string? Carrier { get; set; }

        #endregion

    }

}
