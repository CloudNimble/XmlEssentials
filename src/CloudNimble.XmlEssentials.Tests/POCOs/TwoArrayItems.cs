using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A collection with two item names, which is illegal.
    /// </summary>
    public sealed class TwoArrayItems
    {

        #region Public Properties

        /// <summary>
        /// Gets the values.
        /// </summary>
        [XmlArray]
        [XmlArrayItem("A")]
        [XmlArrayItem("B")]
        public List<int>? Values { get; set; }

        #endregion

    }

}
