using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Maps a member to a child element of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR type written with this element name.</typeparam>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class XmlElementAttribute<T> : XmlElementAttribute
    {

        #region Public Properties

        /// <inheritdoc />
        public override Type Type => typeof(T);

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the attribute with the member name as the element name.
        /// </summary>
        public XmlElementAttribute()
        {
        }

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="elementName">The element local name.</param>
        public XmlElementAttribute(string elementName)
            : base(elementName)
        {
        }

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="elementName">The element local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        public XmlElementAttribute(string elementName, string? namespaceUri)
            : base(elementName, namespaceUri)
        {
        }

        #endregion

    }

}
