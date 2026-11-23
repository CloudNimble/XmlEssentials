using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Sets the XML local name of a property or field, analogous to <c>JsonPropertyNameAttribute</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlPropertyNameAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets the XML local name.
        /// </summary>
        public string Name { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="name">The XML local name.</param>
        public XmlPropertyNameAttribute(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name;
        }

        #endregion

    }

}
