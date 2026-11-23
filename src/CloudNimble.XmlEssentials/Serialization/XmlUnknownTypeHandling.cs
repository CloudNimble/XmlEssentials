namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies how a value declared as <see cref="object"/> that has no converter is handled during deserialization.
    /// </summary>
    public enum XmlUnknownTypeHandling
    {

        /// <summary>
        /// Deserialization as a <see cref="CloudNimble.Text.Xml.XmlElement"/>.
        /// </summary>
        XmlElement = 0,

        /// <summary>
        /// Deserialization as a <see cref="CloudNimble.Text.Xml.Nodes.XmlNode"/>.
        /// </summary>
        XmlNode = 1,

    }

}
