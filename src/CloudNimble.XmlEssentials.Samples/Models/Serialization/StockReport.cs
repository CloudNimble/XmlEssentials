using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A stock report that declares its collections with several different collection types.
    /// </summary>
    public sealed class StockReport
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the counts, as an array.
        /// </summary>
        public int[]? Counts { get; set; }

        /// <summary>
        /// Gets or sets the warehouse codes, as a sequence.
        /// </summary>
        public IEnumerable<string>? Warehouses { get; set; }

        /// <summary>
        /// Gets or sets the stock keeping units, as a read-only list.
        /// </summary>
        public IReadOnlyList<string>? Skus { get; set; }

        /// <summary>
        /// Gets or sets the tags, as a set.
        /// </summary>
        public HashSet<string>? Tags { get; set; }

        /// <summary>
        /// Gets or sets the notes, as a collection class.
        /// </summary>
        public Collection<string>? Notes { get; set; }

        #endregion

    }

}
