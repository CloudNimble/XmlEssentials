using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies the element name of the items of type <typeparamref name="T"/> in a collection member. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the items that the element name applies to.
    /// </typeparam>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class XmlArrayItemAttribute<T> : XmlArrayItemAttribute
    {

        #region Public Properties

        /// <inheritdoc />
        public override Type Type => typeof(T);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlArrayItemAttribute{T}"/> class that uses the default item element name.
        /// </summary>
        public XmlArrayItemAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlArrayItemAttribute{T}"/> class with the specified item element name.
        /// </summary>
        /// <param name="elementName">The local name of the item elements.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="elementName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="elementName"/> is empty or consists only of white-space characters.
        /// </exception>
        public XmlArrayItemAttribute(string elementName)
            : base(elementName)
        {
        }

        #endregion

    }

}
