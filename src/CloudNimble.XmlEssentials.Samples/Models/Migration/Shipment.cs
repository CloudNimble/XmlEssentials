using System;
using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// A shipment with no mapping attributes, used to show the default output.
    /// </summary>
    public sealed class Shipment
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the carrier's name.
        /// </summary>
        public string? Carrier { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the shipment is insured.
        /// </summary>
        public bool Insured { get; set; }

        /// <summary>
        /// Gets or sets the package labels.
        /// </summary>
        public List<string> Packages { get; set; } = [];

        /// <summary>
        /// Gets or sets the time the shipment left the warehouse.
        /// </summary>
        public DateTime ShippedOn { get; set; }

        /// <summary>
        /// Gets or sets where the shipment is.
        /// </summary>
        public ShipmentStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the tracking number, or <see langword="null"/> when the carrier has not assigned one.
        /// </summary>
        public string? TrackingNumber { get; set; }

        /// <summary>
        /// Gets or sets the weight, in kilograms.
        /// </summary>
        public decimal Weight { get; set; }

        #endregion

    }

}
