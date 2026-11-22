using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A value whose converter serializes its content with a nested call to the serializer.
    /// </summary>
    [XmlConverter(typeof(EnvelopeConverter))]
    public readonly struct Envelope
    {

        #region Public Properties

        /// <summary>
        /// Gets the content.
        /// </summary>
        public int Content { get; init; }

        #endregion

    }

}
