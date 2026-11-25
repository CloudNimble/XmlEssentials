using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies that a member is serialized as a child element, and optionally sets the name, namespace, order, or nillability of that element.
    /// </summary>
    /// <remarks>
    /// A member that carries more than one <see cref="XmlElementAttribute{T}"/> holds a choice of elements: each
    /// element name stands for a type. A value is written under the element of its type, and an element is read
    /// as the type of its name. On a collection, each item is written under the element of its own type, in the
    /// order of the collection. A value of the member's own type is written under the member's name.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class XmlElementAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the local name of the element.
        /// </summary>
        /// <value>
        /// The local name of the element. If the value is <see langword="null"/> or empty, the member name is used,
        /// converted by the element naming policy. The default is <see langword="null"/>.
        /// </value>
        /// <remarks>
        /// An <see cref="XmlPropertyNameAttribute"/> on the same member takes precedence over this property.
        /// </remarks>
        public string? ElementName { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the member is nillable.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the member is nillable; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// A member whose type is a reference type, a <see cref="Nullable{T}"/>, or a collection is nillable whatever this value is.
        /// </remarks>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Gets or sets the namespace URI of the element.
        /// </summary>
        /// <value>
        /// The namespace URI of the element. If the value is <see langword="null"/>, the element is written with an unqualified name,
        /// so it is in the default namespace that is in scope. An empty string puts the element in no namespace.
        /// The default is <see langword="null"/>.
        /// </value>
        /// <remarks>
        /// An element in no namespace is written with <c>xmlns=""</c> where a default namespace is in scope, and
        /// only an element in no namespace is read as the member.
        /// </remarks>
        public string? Namespace { get; set; }

        /// <summary>
        /// Gets or sets the order of the element among the child elements of the containing type.
        /// </summary>
        /// <value>
        /// The order of the element. Lower values are written first. A negative value means the order is not set. The default is <c>-1</c>.
        /// </value>
        /// <remarks>
        /// An <see cref="XmlPropertyOrderAttribute"/> on the same member takes precedence over this property.
        /// </remarks>
        public int Order { get; set; } = -1;

        /// <summary>
        /// Gets the type that is serialized with <see cref="ElementName"/>.
        /// </summary>
        /// <value>
        /// The type that the element name applies to, or <see langword="null"/> if the type of the member is used.
        /// </value>
        public virtual Type? Type => null;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlElementAttribute"/> class that uses the member name as the element name.
        /// </summary>
        public XmlElementAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlElementAttribute"/> class with the specified element name.
        /// </summary>
        /// <param name="elementName">The local name of the element.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="elementName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="elementName"/> is empty or consists only of white-space characters.
        /// </exception>
        public XmlElementAttribute(string elementName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(elementName);
            ElementName = elementName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlElementAttribute"/> class with the specified element name and namespace.
        /// </summary>
        /// <param name="elementName">The local name of the element.</param>
        /// <param name="namespaceUri">The namespace URI of the element, or <see langword="null"/> to write the element with an unqualified name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="elementName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="elementName"/> is empty or consists only of white-space characters.
        /// </exception>
        public XmlElementAttribute(string elementName, string? namespaceUri)
            : this(elementName)
        {
            Namespace = namespaceUri;
        }

        #endregion

    }

}
