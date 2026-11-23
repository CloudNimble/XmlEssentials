using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Wraps a collection in a parent element. Without this attribute, items are repeated using the member name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlArrayAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the wrapper element local name. When null or empty, the member name after the naming policy is used.
        /// </summary>
        public string? ElementName { get; set; }

        /// <summary>
        /// Gets or sets whether a null collection is nillable.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Gets or sets the namespace URI of the wrapper.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Gets or sets the order among child elements. The default of <c>-1</c> means unset.
        /// </summary>
        public int Order { get; set; } = -1;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the attribute with the member name as the wrapper name.
        /// </summary>
        public XmlArrayAttribute()
        {
        }

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="elementName">The wrapper element local name.</param>
        public XmlArrayAttribute(string elementName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(elementName);
            ElementName = elementName;
        }

        #endregion

    }

}
