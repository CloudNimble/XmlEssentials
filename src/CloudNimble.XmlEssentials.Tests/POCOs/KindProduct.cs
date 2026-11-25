using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A product whose derived type is written as a <c>kind</c> attribute.
    /// </summary>
    [XmlPolymorphic(TypeDiscriminatorPropertyName = "kind", DiscriminatorPlacement = XmlDiscriminatorPlacement.Attribute)]
    [XmlDerivedType<KindBook>("book")]
    [XmlDerivedType<KindDvd>("dvd")]
    public class KindProduct
    {

        #region Public Properties

        /// <summary>
        /// Gets the title.
        /// </summary>
        public string? Title { get; set; }

        #endregion

    }

}
