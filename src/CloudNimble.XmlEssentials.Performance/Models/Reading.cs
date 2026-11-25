using System;
using Bcl = System.Xml.Serialization;
using Xe = CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// An object mapped entirely as attributes.
    /// </summary>
    public sealed class Reading
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets when the reading was taken.
        /// </summary>
        [Bcl.XmlAttribute]
        [Xe.XmlAttribute]
        public DateTime At { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [Bcl.XmlAttribute]
        [Xe.XmlAttribute]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the sensor name.
        /// </summary>
        [Bcl.XmlAttribute]
        [Xe.XmlAttribute]
        public string? Sensor { get; set; }

        /// <summary>
        /// Gets or sets the measured value.
        /// </summary>
        [Bcl.XmlAttribute]
        [Xe.XmlAttribute]
        public double Value { get; set; }

        #endregion

    }

}
