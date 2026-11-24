using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds collections in wrappers, whose items are named for their types.
    /// </summary>
    public sealed class NamedLineHolder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets lines, whose type has a name of its own.
        /// </summary>
        [XmlArray]
        public List<NamedLine> Lines { get; set; } = [];

        /// <summary>
        /// Gets or sets numbers.
        /// </summary>
        [XmlArray]
        public List<int> Numbers { get; set; } = [];

        /// <summary>
        /// Gets or sets lists of words.
        /// </summary>
        [XmlArray]
        public List<string[]> Words { get; set; } = [];

        #endregion

    }

}
