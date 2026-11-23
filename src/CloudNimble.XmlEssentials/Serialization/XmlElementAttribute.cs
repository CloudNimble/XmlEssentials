using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Maps a member to a child element and optionally sets its name, namespace, order, or nillability.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class XmlElementAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the element local name. When null or empty, the member name after the naming policy is used.
        /// </summary>
        public string? ElementName { get; set; }

        /// <summary>
        /// Gets or sets whether a null value is nillable.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Gets or sets the namespace URI of the element.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Gets or sets the order among child elements. The default of <c>-1</c> means unset.
        /// </summary>
        public int Order { get; set; } = -1;

        /// <summary>
        /// Gets the CLR type for this element name, or null when the member type is used.
        /// </summary>
        public virtual Type? Type => null;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the attribute with the member name as the element name.
        /// </summary>
        public XmlElementAttribute()
        {
        }

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="elementName">The element local name.</param>
        public XmlElementAttribute(string elementName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(elementName);
            ElementName = elementName;
        }

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="elementName">The element local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        public XmlElementAttribute(string elementName, string? namespaceUri)
            : this(elementName)
        {
            Namespace = namespaceUri;
        }

        #endregion

    }

}
