using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Paths
{

    /// <summary>
    /// An order with a member of every kind an error can be in: an attribute, a collection in a wrapper, a
    /// collection with no wrapper, a dictionary, items of derived types, text, and a member with a converter.
    /// </summary>
    public sealed class PathOrder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the counts, a dictionary whose entries are elements named for their keys.
        /// </summary>
        public Dictionary<string, int> Counts { get; set; } = [];

        /// <summary>
        /// Gets or sets the extra lines, a collection with no wrapper.
        /// </summary>
        public List<PathLine> Extras { get; set; } = [];

        /// <summary>
        /// Gets or sets the number of the order, an attribute.
        /// </summary>
        [XmlAttribute]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the lines, a collection in a wrapper.
        /// </summary>
        [XmlArray]
        public List<PathLine> Lines { get; set; } = [];

        /// <summary>
        /// Gets or sets the note, an element with an attribute and text.
        /// </summary>
        public PathNote? Note { get; set; }

        /// <summary>
        /// Gets or sets a value that a converter reads and writes.
        /// </summary>
        [XmlConverter<PathOddConverter>]
        public PathOdd? Odd { get; set; }

        /// <summary>
        /// Gets or sets values that a converter reads and writes, in a wrapper.
        /// </summary>
        [XmlArray]
        public List<PathOdd> Odds { get; set; } = [];

        /// <summary>
        /// Gets or sets the shapes, items of derived types in a wrapper.
        /// </summary>
        [XmlArray]
        public List<PathShape> Shapes { get; set; } = [];

        #endregion

    }

}
