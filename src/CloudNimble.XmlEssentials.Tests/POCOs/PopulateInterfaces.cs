using System.Collections.Generic;
using System.Collections.ObjectModel;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose collections are declared as interfaces.
    /// </summary>
    public sealed class PopulateInterfaces
    {

        #region Public Properties

        /// <summary>
        /// Gets a collection that can take items.
        /// </summary>
        [XmlArray]
        public ICollection<int> Collection { get; } = new List<int> { 1 };

        /// <summary>
        /// Gets a dictionary that can take entries.
        /// </summary>
        public IDictionary<string, int> Entries { get; } = new Dictionary<string, int> { ["a"] = 1 };

        /// <summary>
        /// Gets or sets a dictionary that holds a read-only one, which cannot take entries.
        /// </summary>
        public IDictionary<string, int> FixedEntries { get; set; } = new ReadOnlyDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

        /// <summary>
        /// Gets or sets a list that holds an array, which cannot take items.
        /// </summary>
        [XmlArray]
        public IList<int> Fixed { get; set; } = new[] { 1 };

        /// <summary>
        /// Gets or sets a list that can take items.
        /// </summary>
        [XmlArray]
        public IList<int> List { get; set; } = new List<int> { 1 };

        /// <summary>
        /// Gets or sets a read-only view, which cannot be populated.
        /// </summary>
        [XmlArray]
        public IReadOnlyList<int> ReadOnly { get; set; } = new List<int> { 1 };

        /// <summary>
        /// Gets a sequence, which cannot be populated.
        /// </summary>
        [XmlArray]
        public IEnumerable<int> Sequence { get; } = new List<int> { 1 };

        #endregion

    }

}
