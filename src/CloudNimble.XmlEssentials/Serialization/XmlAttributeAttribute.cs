using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Maps a member to an XML attribute. Simple types only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlAttributeAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the attribute local name. When null or empty, the member name after the naming policy is used.
        /// </summary>
        public string? AttributeName { get; set; }

        /// <summary>
        /// Gets or sets the namespace URI of the attribute.
        /// </summary>
        public string? Namespace { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the attribute with the member name as the attribute name.
        /// </summary>
        public XmlAttributeAttribute()
        {
        }

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="attributeName">The attribute local name.</param>
        public XmlAttributeAttribute(string attributeName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(attributeName);
            AttributeName = attributeName;
        }

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="attributeName">The attribute local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        public XmlAttributeAttribute(string attributeName, string? namespaceUri)
            : this(attributeName)
        {
            Namespace = namespaceUri;
        }

        #endregion

    }

}
