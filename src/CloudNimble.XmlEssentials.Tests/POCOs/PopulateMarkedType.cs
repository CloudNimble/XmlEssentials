using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type marked to have its members populated, one of which cannot be.
    /// </summary>
    [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)]
    public sealed class PopulateMarkedType
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets an array, which cannot be populated.
        /// </summary>
        [XmlArray]
        public int[] Array { get; set; } = [1];

        /// <summary>
        /// Gets a list.
        /// </summary>
        [XmlArray]
        public List<int> Items { get; } = [1];

        /// <summary>
        /// Gets or sets a string, which cannot be populated.
        /// </summary>
        public string Name { get; set; } = "n";

        #endregion

    }

}
