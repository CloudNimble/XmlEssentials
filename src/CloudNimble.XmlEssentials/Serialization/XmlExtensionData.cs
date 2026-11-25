using System.Collections.Generic;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Represents the XML attributes and child elements that have no matching member and are captured by a member marked with
    /// <see cref="XmlExtensionDataAttribute"/>. This class cannot be inherited.
    /// </summary>
    public sealed class XmlExtensionData
    {

        #region Public Properties

        /// <summary>
        /// Gets the XML attributes that have no matching member.
        /// </summary>
        /// <value>
        /// A dictionary that maps the name of each unmapped attribute to its value.
        /// </value>
        public Dictionary<XmlName, string> Attributes { get; } = [];

        /// <summary>
        /// Gets the child elements that have no matching member.
        /// </summary>
        /// <value>
        /// A list of the unmapped child elements, in document order.
        /// </value>
        public List<XmlElementNode> Elements { get; } = [];

        #endregion

    }

}
