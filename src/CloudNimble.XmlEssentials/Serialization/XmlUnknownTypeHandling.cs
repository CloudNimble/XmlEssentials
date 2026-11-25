namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Defines how <see cref="XmlSerializer"/> reads an element into a value declared as <see cref="object"/>.
    /// </summary>
    /// <remarks>
    /// A document does not record the type a value had when it was written, so a value declared as
    /// <see cref="object"/> is read as XML. This is the counterpart of
    /// <c>System.Text.Json.Serialization.JsonUnknownTypeHandling</c>, whose values are <c>JsonElement</c> and
    /// <c>JsonNode</c>.
    /// </remarks>
    public enum XmlUnknownTypeHandling
    {

        /// <summary>
        /// The element is read as an <see cref="CloudNimble.Text.Xml.XmlElement"/>, which is read-only.
        /// </summary>
        XmlElement = 0,

        /// <summary>
        /// The element is read as an <see cref="CloudNimble.Text.Xml.Nodes.XmlNode"/>, which can be changed. The node is an
        /// <see cref="CloudNimble.Text.Xml.Nodes.XmlElementNode"/>.
        /// </summary>
        XmlNode = 1,

    }

}
