using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A product whose derived type is written as <c>xsi:type</c>.
    /// </summary>
    [XmlPolymorphic(DiscriminatorPlacement = XmlDiscriminatorPlacement.XsiType)]
    [XmlDerivedType<XsiBook>("Book")]
    [XmlDerivedType<XsiDvd>("Dvd")]
    public class XsiProduct
    {

        #region Public Properties

        /// <summary>
        /// Gets the title.
        /// </summary>
        public string? Title { get; set; }

        #endregion

    }

}
