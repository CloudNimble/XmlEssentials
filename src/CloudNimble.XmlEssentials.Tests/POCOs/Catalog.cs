using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A catalog with an unwrapped list of items.
    /// </summary>
    public sealed class Catalog
    {

        #region Public Properties

        /// <summary>
        /// Gets the items.
        /// </summary>
        public List<string>? Items { get; set; }

        /// <summary>
        /// Gets the title.
        /// </summary>
        public string? Title { get; set; }

        #endregion

    }

}
