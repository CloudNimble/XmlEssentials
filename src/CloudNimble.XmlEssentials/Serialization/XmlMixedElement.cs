using CloudNimble.Text.Xml.Nodes;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Represents a child element in mixed content. This class cannot be inherited.
    /// </summary>
    public sealed class XmlMixedElement : XmlMixedContentItem
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the child element.
        /// </summary>
        /// <value>
        /// The child element, or <see langword="null"/> if none is set. An item whose element is <see langword="null"/> is not written.
        /// The default is <see langword="null"/>.
        /// </value>
        public XmlElementNode? Element { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlMixedElement"/> class that has no element.
        /// </summary>
        public XmlMixedElement()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlMixedElement"/> class with the specified element.
        /// </summary>
        /// <param name="element">The child element.</param>
        public XmlMixedElement(XmlElementNode element)
        {
            Element = element;
        }

        #endregion

    }

}
