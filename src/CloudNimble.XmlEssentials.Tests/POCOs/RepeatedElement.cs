using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with two <see cref="XmlElementAttribute"/> annotations on one member, which object mapping does not yet honor.
    /// </summary>
    public sealed class RepeatedElement
    {

        #region Public Properties

        /// <summary>
        /// Gets a value that would be a polymorphic element choice.
        /// </summary>
        [XmlElement("A")]
        [XmlElement("B")]
        public string? Value { get; set; }

        #endregion

    }

}
