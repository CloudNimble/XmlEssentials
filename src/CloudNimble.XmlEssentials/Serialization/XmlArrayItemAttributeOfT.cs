using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Sets the element name of collection items of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR item type.</typeparam>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class XmlArrayItemAttribute<T> : XmlArrayItemAttribute
    {

        #region Public Properties

        /// <inheritdoc />
        public override Type Type => typeof(T);

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
            : base(elementName)
        {
        }

        #endregion

    }

}
