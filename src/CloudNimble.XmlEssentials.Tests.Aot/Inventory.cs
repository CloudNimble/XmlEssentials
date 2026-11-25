using System.Collections.Generic;
using System.Collections.Immutable;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.Aot
{

    /// <summary>
    /// Holds one member of each kind that a generated context describes to the runtime library: a dictionary,
    /// collections that are not lists, a collection with no setter, an XML value, and a value declared as object.
    /// </summary>
    public sealed class Inventory
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value declared as object.
        /// </summary>
        public object? Any { get; set; }

        /// <summary>
        /// Gets or sets an array.
        /// </summary>
        public int[] Codes { get; set; } = [];

        /// <summary>
        /// Gets or sets a dictionary with a value-type value.
        /// </summary>
        public Dictionary<string, int> Counts { get; set; } = [];

        /// <summary>
        /// Gets a list with no setter, which is populated when it is read.
        /// </summary>
        [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)]
        public List<string> Notes { get; } = [];

        /// <summary>
        /// Gets or sets a queue.
        /// </summary>
        public Queue<int> Pending { get; set; } = new();

        /// <summary>
        /// Gets or sets an element of a parsed document.
        /// </summary>
        public XmlElement Raw { get; set; }

        /// <summary>
        /// Gets or sets an immutable array.
        /// </summary>
        public ImmutableArray<int> Sizes { get; set; } = [];

        /// <summary>
        /// Gets or sets a set.
        /// </summary>
        public HashSet<string> Tags { get; set; } = [];

        #endregion

    }

}
