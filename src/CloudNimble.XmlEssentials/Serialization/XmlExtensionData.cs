using System.Collections.Generic;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Unmapped attributes and elements captured by <see cref="XmlExtensionDataAttribute"/>.
    /// </summary>
    public sealed class XmlExtensionData
    {

        #region Public Properties

        /// <summary>
        /// Gets unmapped attributes keyed by XML name.
        /// </summary>
        public Dictionary<XmlName, string> Attributes { get; } = [];

        /// <summary>
        /// Gets unmapped child elements.
        /// </summary>
        public List<XmlElementNode> Elements { get; } = [];

        #endregion

    }

}
