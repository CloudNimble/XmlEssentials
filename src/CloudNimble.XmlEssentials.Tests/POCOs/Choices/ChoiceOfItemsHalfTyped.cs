using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Choices
{

    /// <summary>
    /// A wrapper with two item names of which one names no type, which is refused.
    /// </summary>
    public sealed class ChoiceOfItemsHalfTyped
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        [XmlArray]
        [XmlArrayItem<int>("a")]
        [XmlArrayItem("b")]
        public List<int> Values { get; set; } = [];

        #endregion

    }

}
