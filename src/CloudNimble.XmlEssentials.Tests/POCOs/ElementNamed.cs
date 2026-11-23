using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that names a child element with <see cref="XmlElementAttribute"/>.
    /// </summary>
    public sealed class ElementNamed
    {

        #region Public Properties

        /// <summary>
        /// Gets the given name.
        /// </summary>
        [XmlElement("Nick")]
        public string? Name { get; set; }

        #endregion

    }

}
