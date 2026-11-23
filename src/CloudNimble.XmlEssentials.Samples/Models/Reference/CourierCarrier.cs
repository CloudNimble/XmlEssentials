namespace CloudNimble.XmlEssentials.Samples.Models.Reference
{

    /// <summary>
    /// A carrier that delivers by courier.
    /// </summary>
    public sealed class CourierCarrier : Carrier
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the vehicle the courier uses.
        /// </summary>
        public string? Vehicle { get; set; }

        #endregion

    }

}
