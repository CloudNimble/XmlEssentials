using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Bcl
{

    /// <summary>
    /// Collection members with each mapping attribute and with none, and two members that hold many values
    /// without being collections of elements.
    /// </summary>
    public class MappedLists
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a dictionary, which is not a collection of items.
        /// </summary>
        public Dictionary<string, int> Counts { get; set; } = [];

        /// <summary>
        /// Gets or sets binary data, which is one value.
        /// </summary>
        public byte[] Data { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection that <c>[XmlArray]</c> wraps.
        /// </summary>
        [XmlArray("w")]
        public List<int> Explicit { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection that <c>[XmlElement]</c> leaves without a wrapper.
        /// </summary>
        [XmlElement("n")]
        public List<int> Flat { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection that is <see langword="null"/>.
        /// </summary>
        public List<int>? Missing { get; set; }

        /// <summary>
        /// Gets or sets a collection whose items are named, with nothing said about a wrapper.
        /// </summary>
        [XmlArrayItem("v")]
        public List<int> Named { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection with no attribute.
        /// </summary>
        public List<int> Plain { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection whose items are typed elements, which have no wrapper.
        /// </summary>
        [XmlElement<int>("t")]
        public List<int> Typed { get; set; } = [];

        #endregion

    }

}
