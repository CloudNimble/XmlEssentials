using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A product that writes unknown derived types as the base type.
    /// </summary>
    [XmlPolymorphic(TypeDiscriminatorPropertyName = "kind", DiscriminatorPlacement = XmlDiscriminatorPlacement.Attribute, UnknownDerivedTypeHandling = XmlUnknownDerivedTypeHandling.FallBackToBaseType)]
    [XmlDerivedType<FallbackBook>("book")]
    public class FallbackProduct
    {

        #region Public Properties

        /// <summary>
        /// Gets the title.
        /// </summary>
        public string? Title { get; set; }

        #endregion

    }

}
