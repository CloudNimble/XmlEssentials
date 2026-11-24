using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Members whose order and nullability come from the typed form of <see cref="XmlElementAttribute"/>.
    /// </summary>
    public sealed class TypedElementFacets
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the member that is declared first and ordered last.
        /// </summary>
        [XmlElement<Person>(Order = 2)]
        public object? Last { get; set; }

        /// <summary>
        /// Gets or sets a value-type member that the schema allows to be nil.
        /// </summary>
        [XmlElement<int>(Order = 1, IsNullable = true)]
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets a value-type member that is not nillable.
        /// </summary>
        [XmlElement<int>(Order = 0)]
        public int First { get; set; }

        #endregion

    }

}
