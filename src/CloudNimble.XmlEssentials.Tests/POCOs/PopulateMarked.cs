using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with one member marked to be populated, one marked to be replaced, and one unmarked.
    /// </summary>
    public sealed class PopulateMarked
    {

        #region Public Properties

        /// <summary>
        /// Gets a list that is populated whatever the options say.
        /// </summary>
        [XmlArray]
        [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)]
        public List<int> Items { get; } = [1];

        /// <summary>
        /// Gets a list that follows the options.
        /// </summary>
        [XmlArray]
        public List<int> Other { get; } = [1];

        /// <summary>
        /// Gets or sets a list that is replaced whatever the options say.
        /// </summary>
        [XmlArray]
        [XmlObjectCreationHandling(XmlObjectCreationHandling.Replace)]
        public List<int> Replaced { get; set; } = [1];

        #endregion

    }

}
