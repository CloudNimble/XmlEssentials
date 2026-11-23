namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// One reading from a sensor.
    /// </summary>
    public sealed class SensorReading
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the sensor.
        /// </summary>
        public string? Sensor { get; set; }

        /// <summary>
        /// Gets or sets the measured value.
        /// </summary>
        public double Value { get; set; }

        #endregion

    }

}
