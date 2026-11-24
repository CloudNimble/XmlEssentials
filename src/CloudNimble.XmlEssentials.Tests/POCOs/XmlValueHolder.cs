using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds members whose values are XML already.
    /// </summary>
    public sealed class XmlValueHolder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets an element of a parsed document.
        /// </summary>
        public XmlElement Element { get; set; }

        /// <summary>
        /// Gets or sets a parsed document.
        /// </summary>
        public XmlDocument? Document { get; set; }

        /// <summary>
        /// Gets or sets a node of a mutable tree.
        /// </summary>
        public XmlNode? Node { get; set; }

        /// <summary>
        /// Gets or sets an element of a mutable tree.
        /// </summary>
        public XmlElementNode? ElementNode { get; set; }

        #endregion

    }

}
