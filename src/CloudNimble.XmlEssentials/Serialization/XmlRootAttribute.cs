using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Sets the document element name and namespace of a type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class XmlRootAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the document element local name. When null or empty, the CLR type name is used.
        /// </summary>
        public string? ElementName { get; set; }

        /// <summary>
        /// Gets or sets whether a null value of this type is nillable. When unset, nillability follows the CLR type.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Gets or sets the namespace URI of the document element.
        /// </summary>
        public string? Namespace { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the attribute with the type name as the document element.
        /// </summary>
        public XmlRootAttribute()
        {
        }

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="elementName">The document element local name.</param>
        public XmlRootAttribute(string elementName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(elementName);
            ElementName = elementName;
        }

        #endregion

    }

}
