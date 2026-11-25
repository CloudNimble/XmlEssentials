using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies that a member is serialized as a child element of type <typeparamref name="T"/>. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">
    /// The type that is serialized with the element name.
    /// </typeparam>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class XmlElementAttribute<T> : XmlElementAttribute
    {

        #region Public Properties

        /// <inheritdoc />
        public override Type Type => typeof(T);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlElementAttribute{T}"/> class that uses the default element name:
        /// the member name, or the element name of <typeparamref name="T"/> when the member has more than one of these attributes.
        /// </summary>
        public XmlElementAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlElementAttribute{T}"/> class with the specified element name.
        /// </summary>
        /// <param name="elementName">The local name of the element.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="elementName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="elementName"/> is empty or consists only of white-space characters.
        /// </exception>
        public XmlElementAttribute(string elementName)
            : base(elementName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlElementAttribute{T}"/> class with the specified element name and namespace.
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
            : base(elementName, namespaceUri)
        {
        }

        #endregion

    }

}
