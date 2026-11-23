using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Reference
{

    /// <summary>
    /// The base type of a carrier. An <c>xsi:type</c> attribute names the derived type.
    /// </summary>
    [XmlPolymorphic(DiscriminatorPlacement = XmlDiscriminatorPlacement.XsiType)]
    [XmlDerivedType<CourierCarrier>("courier")]
    public class Carrier
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the carrier.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
