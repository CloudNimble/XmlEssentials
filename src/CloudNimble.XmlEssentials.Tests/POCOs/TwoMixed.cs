using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with two mixed-content members, which is illegal.
    /// </summary>
    public sealed class TwoMixed
    {

        #region Public Properties

        /// <summary>
        /// Gets the first mixed member.
        /// </summary>
        [XmlMixed]
        public List<XmlMixedContentItem> A { get; set; } = [];

        /// <summary>
        /// Gets the second mixed member.
        /// </summary>
        [XmlMixed]
        public List<XmlMixedContentItem> B { get; set; } = [];

        #endregion

    }

}
