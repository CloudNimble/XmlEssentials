using System;
using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// A shipment mapped so that it reads a document that <c>System.Xml.Serialization.XmlSerializer</c> wrote with its defaults.
    /// </summary>
    [XmlRoot("Shipment")]
    public sealed class ArchivedShipment
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the carrier's name.
        /// </summary>
        public string? Carrier { get; set; }

        /// <summary>
        /// Gets or sets the package labels, read from <c>string</c> elements inside a <c>Packages</c> element.
        /// </summary>
        [XmlArray]
        [XmlArrayItem("string")]
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
        /// Gets or sets the tracking number, which the old document leaves out when it is <see langword="null"/>.
        /// </summary>
        public string? TrackingNumber { get; set; }

        #endregion

    }

}
