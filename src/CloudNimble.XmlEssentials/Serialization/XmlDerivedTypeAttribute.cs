using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Declares a derived type that can appear where the annotated type is expected.
    /// Use <see cref="XmlDerivedTypeAttribute{T}"/> to specify the derived type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public abstract class XmlDerivedTypeAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets the derived CLR type.
        /// </summary>
        public abstract Type DerivedType { get; }

        /// <summary>
        /// Gets or sets the namespace URI of the discriminator element.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Gets the discriminator (usually the element local name).
        /// </summary>
        public object? TypeDiscriminator { get; protected set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        public XmlDerivedTypeAttribute()
        {
        }

        #endregion

    }

}
