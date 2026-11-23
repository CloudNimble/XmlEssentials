using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A catalog whose items are wrapped by <see cref="XmlArrayAttribute"/>.
    /// </summary>
    public sealed class WrappedCatalog
    {

        #region Public Properties

        /// <summary>
        /// Gets the items.
        /// </summary>
        [XmlArray("Items", Order = 1)]
        [XmlArrayItem("Item")]
        public List<string>? Items { get; set; }

        /// <summary>
        /// Gets the title.
        /// </summary>
        public string? Title { get; set; }

        #endregion

    }

}
