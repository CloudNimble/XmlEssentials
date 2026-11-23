using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies the element name, and optionally the type, of the items in a collection member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class XmlArrayItemAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the local name of the item elements.
        /// </summary>
        /// <value>
        /// The local name of the item elements. If the value is <see langword="null"/> or empty, the name of the item type is used
        /// for a collection wrapped by <see cref="XmlArrayAttribute"/>, converted by the element naming policy, and the member's
        /// element name is used for a collection that is not wrapped. The default is <see langword="null"/>.
        /// </value>
        public string? ElementName { get; set; }

        /// <summary>
        /// Gets or sets the namespace URI of the item elements.
        /// </summary>
        /// <value>
        /// The namespace URI of the item elements. The default is <see langword="null"/>.
        /// </value>
        /// <remarks>
        /// The serializer does not read this property.
        /// </remarks>
        public string? Namespace { get; set; }

        /// <summary>
        /// Gets the item type that <see cref="ElementName"/> applies to.
        /// </summary>
        /// <value>
        /// The item type, or <see langword="null"/> if the element type of the collection is used.
        /// </value>
        public virtual Type? Type => null;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlArrayItemAttribute"/> class that uses the default item element name.
        /// </summary>
        public XmlArrayItemAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlArrayItemAttribute"/> class with the specified item element name.
        /// </summary>
        /// <param name="elementName">The local name of the item elements.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="elementName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="elementName"/> is empty or consists only of white-space characters.
        /// </exception>
        public XmlArrayItemAttribute(string elementName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(elementName);
            ElementName = elementName;
        }

        #endregion

    }

}
