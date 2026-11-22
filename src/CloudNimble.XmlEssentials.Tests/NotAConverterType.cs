using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A type whose <see cref="XmlConverterAttribute"/> names a type that is not a converter.
    /// </summary>
    [XmlConverter(typeof(string))]
    public readonly struct NotAConverterType
    {

        #region Public Properties

        /// <summary>
        /// Gets a dummy value.
        /// </summary>
        public int Value { get; init; }

        #endregion

    }

}
