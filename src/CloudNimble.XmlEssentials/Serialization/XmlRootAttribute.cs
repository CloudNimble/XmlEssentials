using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies the name and namespace of the document element that a type is serialized as. This class cannot be inherited.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class XmlRootAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the local name of the document element.
        /// </summary>
        /// <value>
        /// The local name of the document element. If the value is <see langword="null"/> or empty, the name of the type is used,
        /// converted by the element naming policy. The default is <see langword="null"/>.
        /// </value>
        public string? ElementName { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether a <see langword="null"/> value of the type is nillable.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if a <see langword="null"/> value of the type is nillable; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// The serializer does not read this property. Nillability follows the type: a reference type or a <see cref="Nullable{T}"/> is nillable.
        /// </remarks>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Gets or sets the namespace URI of the document element.
        /// </summary>
        /// <value>
        /// The namespace URI of the document element. If the value is <see langword="null"/>, <see cref="XmlSerializerOptions.DefaultNamespace"/> is used.
        /// The default is <see langword="null"/>.
        /// </value>
        public string? Namespace { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlRootAttribute"/> class that uses the name of the type as the document element name.
        /// </summary>
        public XmlRootAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlRootAttribute"/> class with the specified document element name.
        /// </summary>
        /// <param name="elementName">The local name of the document element.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="elementName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="elementName"/> is empty or consists only of white-space characters.
        /// </exception>
        public XmlRootAttribute(string elementName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(elementName);
            ElementName = elementName;
        }

        #endregion

    }

}
