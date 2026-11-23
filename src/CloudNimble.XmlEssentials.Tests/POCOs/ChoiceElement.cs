using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that uses a typed <see cref="XmlElementAttribute{T}"/> for per-member element choice.
    /// </summary>
    public sealed class ChoiceElement
    {

        #region Public Properties

        /// <summary>
        /// Gets a value whose element name would discriminate a derived type.
        /// </summary>
        [XmlElement<Person>("P")]
        public object? Item { get; set; }

        #endregion

    }

}
