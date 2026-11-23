using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Sets the element name (and optional type) of items in a collection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class XmlArrayItemAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the item element local name. When null or empty, the item CLR type name is used for a wrapped collection, or the member name when unwrapped.
        /// </summary>
        public string? ElementName { get; set; }

        /// <summary>
        /// Gets or sets the namespace URI of the item.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Gets the CLR item type for this element name, or null when the collection element type is used.
        /// </summary>
        public virtual Type? Type => null;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the attribute with the default item name.
        /// </summary>
        public XmlArrayItemAttribute()
        {
        }

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="elementName">The item element local name.</param>
        public XmlArrayItemAttribute(string elementName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(elementName);
            ElementName = elementName;
        }

        #endregion

    }

}
