using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A member with both a name-only and a typed <see cref="XmlElementAttribute"/>, which is illegal.
    /// </summary>
    public sealed class TypedAndNamedElement
    {

        #region Public Properties

        /// <summary>
        /// Gets the value.
        /// </summary>
        [XmlElement("X")]
        [XmlElement<Person>]
        public object? Item { get; set; }

        #endregion

    }

}
