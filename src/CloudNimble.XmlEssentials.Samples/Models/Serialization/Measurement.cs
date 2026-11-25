namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A named measurement with read-only properties that are set by the constructor.
    /// </summary>
    public sealed class Measurement
    {

        #region Public Properties

        /// <summary>
        /// Gets the name of the quantity that was measured.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the measured value.
        /// </summary>
        public double Value { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Measurement"/> class.
        /// </summary>
        /// <param name="name">The name of the quantity that was measured.</param>
        /// <param name="value">The measured value.</param>
        public Measurement(string name, double value)
        {
            Name = name;
            Value = value;
        }

        #endregion

    }

}
