using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// A catalog entry with no mapping attributes, the way a type written for <c>System.Text.Json</c> looks.
    /// </summary>
    public sealed class CatalogItem
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the description, or <see langword="null"/> when there is none.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the entry's number.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the entry is in stock.
        /// </summary>
        public bool InStock { get; set; }

        /// <summary>
        /// Gets or sets the search tags.
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// Gets or sets the unit price.
        /// </summary>
        public decimal UnitPrice { get; set; }

        #endregion

    }

}
