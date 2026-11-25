using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A polymorphic type that omits the required discriminator attribute name.
    /// </summary>
    [XmlPolymorphic(DiscriminatorPlacement = XmlDiscriminatorPlacement.Attribute)]
    public class BadKindProduct
    {

        #region Public Properties

        /// <summary>
        /// Gets the title.
        /// </summary>
        public string? Title { get; set; }

        #endregion

    }

}
