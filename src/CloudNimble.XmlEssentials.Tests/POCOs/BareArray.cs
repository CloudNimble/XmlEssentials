using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a wrapper collection using the parameterless <see cref="XmlArrayAttribute"/>.
    /// </summary>
    public sealed class BareArray
    {

        #region Public Properties

        /// <summary>
        /// Gets the values.
        /// </summary>
        [XmlArray]
        [XmlArrayItem<int>]
        public List<int>? Values { get; set; }

        #endregion

    }

}
