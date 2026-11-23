using CloudNimble.Text.Xml.Nodes;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// A child element in mixed content.
    /// </summary>
    public sealed class XmlMixedElement : XmlMixedContentItem
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the element.
        /// </summary>
        public XmlElementNode? Element { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an empty mixed element.
        /// </summary>
        public XmlMixedElement()
        {
        }

        /// <summary>
        /// Creates a mixed element.
        /// </summary>
        /// <param name="element">The child element.</param>
        public XmlMixedElement(XmlElementNode element)
        {
            Element = element;
        }

        #endregion

    }

}
