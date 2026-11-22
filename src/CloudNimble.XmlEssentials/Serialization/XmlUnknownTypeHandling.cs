namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// How a member of type <see cref="object"/> is materialized when it has no converter.
    /// </summary>
    public enum XmlUnknownTypeHandling
    {

        /// <summary>
        /// Materialize as <see cref="CloudNimble.Text.Xml.XmlElement"/>.
        /// </summary>
        XmlElement = 0,

        /// <summary>
        /// Materialize as <c>CloudNimble.Text.Xml.Nodes.XmlNode</c>.
        /// </summary>
        XmlNode = 1,

    }

}
