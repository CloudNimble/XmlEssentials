using System.Text.Json.Serialization;
using Xe = CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// The base of a polymorphic list. Derived types are told apart by element name.
    /// </summary>
    [JsonDerivedType(typeof(Circle), nameof(Circle))]
    [JsonDerivedType(typeof(Square), nameof(Square))]
    [Xe.XmlDerivedType<Circle>]
    [Xe.XmlDerivedType<Square>]
    public class Shape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
