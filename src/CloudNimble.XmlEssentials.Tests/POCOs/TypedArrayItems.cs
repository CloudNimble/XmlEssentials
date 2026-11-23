using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A wrapped collection whose items use <see cref="XmlArrayItemAttribute{T}"/>.
    /// </summary>
    public sealed class TypedArrayItems
    {

        #region Public Properties

        /// <summary>
        /// Gets the values.
        /// </summary>
        [XmlArray]
        [XmlArrayItem<int>("N")]
        public List<int>? Values { get; set; }

        #endregion

    }

}
