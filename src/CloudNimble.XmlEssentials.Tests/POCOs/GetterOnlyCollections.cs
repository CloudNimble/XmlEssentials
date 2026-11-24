using System.Collections.Generic;
using System.Collections.ObjectModel;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Collections that have no setter: the ones that can take items are filled, and the others are left as they are.
    /// </summary>
    public sealed class GetterOnlyCollections
    {

        #region Public Properties

        /// <summary>
        /// Gets a list written as repeated elements.
        /// </summary>
        public List<int> Items { get; } = [];

        /// <summary>
        /// Gets a list written inside a wrapper element.
        /// </summary>
        [XmlArray]
        public List<string> Names { get; } = [];

        /// <summary>
        /// Gets a set.
        /// </summary>
        public HashSet<int> Set { get; } = [];

        /// <summary>
        /// Gets a collection that is null, so there is nothing to add to.
        /// </summary>
        public List<int>? Missing { get; }

        /// <summary>
        /// Gets an array, which cannot grow.
        /// </summary>
        public int[] Fixed { get; } = [0];

        /// <summary>
        /// Gets a collection that is read-only.
        /// </summary>
        public ReadOnlyCollection<int> Frozen { get; } = new([9]);

        #endregion

    }

}
