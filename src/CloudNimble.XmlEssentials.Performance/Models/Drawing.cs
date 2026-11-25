using System.Collections.Generic;
using Bcl = System.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// Holds a polymorphic list of <see cref="Shape"/>.
    /// </summary>
    public sealed class Drawing
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the shapes, each written under the element name of its runtime type.
        /// </summary>
        [Bcl.XmlElement("Shapes", typeof(Shape))]
        [Bcl.XmlElement("Circle", typeof(Circle))]
        [Bcl.XmlElement("Square", typeof(Square))]
        public List<Shape>? Shapes { get; set; }

        #endregion

    }

}
