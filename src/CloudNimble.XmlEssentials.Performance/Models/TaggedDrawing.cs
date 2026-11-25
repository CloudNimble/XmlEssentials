using System.Collections.Generic;
using Bcl = System.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// A list of <see cref="TaggedShape"/> values of several derived types.
    /// </summary>
    public sealed class TaggedDrawing
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the shapes, written with no wrapper.
        /// </summary>
        [Bcl.XmlElement("Shapes")]
        public List<TaggedShape>? Shapes { get; set; }

        #endregion

    }

}
