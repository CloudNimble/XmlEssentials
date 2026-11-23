using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that uses a typed <see cref="XmlElementAttribute{T}"/> with the member name.
    /// </summary>
    public sealed class TypeOnlyElement
    {

        #region Public Properties

        /// <summary>
        /// Gets a value whose element would discriminate a derived type.
        /// </summary>
        [XmlElement<Person>]
        public object? Item { get; set; }

        #endregion

    }

}
