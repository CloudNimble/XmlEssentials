using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies that a member is serialized as an XML attribute. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// The member must have a simple type: a type with a built-in value converter, an enumeration, or the nullable form of
    /// either. A member of any other type cannot be serialized as an XML attribute.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlAttributeAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the local name of the XML attribute.
        /// </summary>
        /// <value>
        /// The local name of the XML attribute. If the value is <see langword="null"/> or empty, the member name is used,
        /// converted by the attribute naming policy. The default is <see langword="null"/>.
        /// </value>
        /// <remarks>
        /// An <see cref="XmlPropertyNameAttribute"/> on the same member takes precedence over this property.
        /// </remarks>
        public string? AttributeName { get; set; }

        /// <summary>
        /// Gets or sets the namespace URI of the XML attribute.
        /// </summary>
        /// <value>
        /// The namespace URI of the XML attribute, or <see langword="null"/> for no namespace. The default is <see langword="null"/>.
        /// </value>
        public string? Namespace { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlAttributeAttribute"/> class that uses the member name as the attribute name.
        /// </summary>
        public XmlAttributeAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlAttributeAttribute"/> class with the specified attribute name.
        /// </summary>
        /// <param name="attributeName">The local name of the XML attribute.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="attributeName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="attributeName"/> is empty or consists only of white-space characters.
        /// </exception>
        public XmlAttributeAttribute(string attributeName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(attributeName);
            AttributeName = attributeName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlAttributeAttribute"/> class with the specified attribute name and namespace.
        /// </summary>
        /// <param name="attributeName">The local name of the XML attribute.</param>
        /// <param name="namespaceUri">The namespace URI of the XML attribute, or <see langword="null"/> for no namespace.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="attributeName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="attributeName"/> is empty or consists only of white-space characters.
        /// </exception>
        public XmlAttributeAttribute(string attributeName, string? namespaceUri)
            : this(attributeName)
        {
            Namespace = namespaceUri;
        }

        #endregion

    }

}
