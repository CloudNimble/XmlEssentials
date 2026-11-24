using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies that a collection member is serialized as a wrapper element that contains the items. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// Without this attribute, the items are written as repeated elements that use the member's element name, with no wrapper.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlArrayAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the local name of the wrapper element.
        /// </summary>
        /// <value>
        /// The local name of the wrapper element. If the value is <see langword="null"/> or empty, the member name is used,
        /// converted by the element naming policy. The default is <see langword="null"/>.
        /// </value>
        /// <remarks>
        /// An <see cref="XmlPropertyNameAttribute"/> on the same member takes precedence over this property.
        /// </remarks>
        public string? ElementName { get; set; }

        /// <summary>
        /// Gets or sets the namespace URI of the wrapper element.
        /// </summary>
        /// <value>
        /// The namespace URI of the wrapper element. If the value is <see langword="null"/>, the namespace set by an
        /// <see cref="XmlElementAttribute"/> on the same member is used, and without one the wrapper element is written with an
        /// unqualified name. An empty string puts the wrapper element in no namespace. The default is <see langword="null"/>.
        /// </value>
        public string? Namespace { get; set; }

        /// <summary>
        /// Gets or sets the order of the wrapper element among the child elements of the containing type.
        /// </summary>
        /// <value>
        /// The order of the wrapper element. Lower values are written first. A negative value means the order is not set. The default is <c>-1</c>.
        /// </value>
        /// <remarks>
        /// The value is used only when neither an <see cref="XmlPropertyOrderAttribute"/> nor an
        /// <see cref="XmlElementAttribute.Order"/> on the same member sets an order.
        /// </remarks>
        public int Order { get; set; } = -1;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlArrayAttribute"/> class that uses the member name as the wrapper element name.
        /// </summary>
        public XmlArrayAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlArrayAttribute"/> class with the specified wrapper element name.
        /// </summary>
        /// <param name="elementName">The local name of the wrapper element.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="elementName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="elementName"/> is empty or consists only of white-space characters.
        /// </exception>
        public XmlArrayAttribute(string elementName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(elementName);
            ElementName = elementName;
        }

        #endregion

    }

}
