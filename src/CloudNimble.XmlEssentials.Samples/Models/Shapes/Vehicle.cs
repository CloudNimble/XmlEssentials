using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// The base type of a vehicle. The derived type is named by an <c>xsi:type</c> attribute, and a type that is not declared is written as its nearest declared ancestor.
    /// </summary>
    [XmlPolymorphic(DiscriminatorPlacement = XmlDiscriminatorPlacement.XsiType, UnknownDerivedTypeHandling = XmlUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
    [XmlDerivedType<Car>]
    [XmlDerivedType<Truck>]
    public class Vehicle
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the manufacturer.
        /// </summary>
        public string? Make { get; set; }

        #endregion

    }

}
