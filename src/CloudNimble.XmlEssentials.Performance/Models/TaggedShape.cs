using System.Text.Json.Serialization;
using CloudNimble.Text.Xml;
using Bcl = System.Xml.Serialization;
using Xe = CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// A base type whose derived types are told apart by a <c>kind</c> attribute.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(TaggedCircle), "circle")]
    [JsonDerivedType(typeof(TaggedSquare), "square")]
    [Xe.XmlPolymorphic(TypeDiscriminatorPropertyName = "kind", DiscriminatorPlacement = XmlDiscriminatorPlacement.Attribute)]
    [Xe.XmlDerivedType<TaggedCircle>("circle")]
    [Xe.XmlDerivedType<TaggedSquare>("square")]
    public class TaggedShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
