using System.Text.Json.Serialization;
using CloudNimble.Text.Xml;
using Bcl = System.Xml.Serialization;
using Xe = CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// A base type whose derived types are told apart by <c>xsi:type</c>.
    /// </summary>
    [Bcl.XmlInclude(typeof(TypedCircle))]
    [Bcl.XmlInclude(typeof(TypedSquare))]
    [Xe.XmlPolymorphic(DiscriminatorPlacement = XmlDiscriminatorPlacement.XsiType)]
    [Xe.XmlDerivedType<TypedCircle>]
    [Xe.XmlDerivedType<TypedSquare>]
    public class TypedShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
