using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose non-nullable members are an attribute, a wrapped list, repeated elements and a dictionary.
    /// </summary>
    public sealed class AnnotatedShapes
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets an attribute annotated as never null.
        /// </summary>
        [XmlAttribute]
        public string Code { get; set; } = "c";

        /// <summary>
        /// Gets or sets repeated elements annotated as never null.
        /// </summary>
        [XmlElement("Item")]
        public List<string> Loose { get; set; } = [];

        /// <summary>
        /// Gets or sets a dictionary annotated as never null.
        /// </summary>
        public Dictionary<string, string> Map { get; set; } = [];

        /// <summary>
        /// Gets or sets a wrapped list annotated as never null.
        /// </summary>
        [XmlArray]
        public List<string> Tags { get; set; } = [];

        #endregion

    }

}
