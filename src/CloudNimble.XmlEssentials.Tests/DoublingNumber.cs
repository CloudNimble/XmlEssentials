using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A type that serializes through <see cref="DoublingNumberConverter"/>.
    /// </summary>
    [XmlConverter<DoublingNumberConverter>]
    public readonly struct DoublingNumber
    {

        #region Public Properties

        /// <summary>
        /// Gets the stored integer.
        /// </summary>
        public int Value { get; init; }

        #endregion

    }

}
