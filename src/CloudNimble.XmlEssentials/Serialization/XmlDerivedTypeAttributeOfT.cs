using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Declares that <typeparamref name="T"/> can appear where the annotated type is expected.
    /// </summary>
    /// <typeparam name="T">The derived CLR type.</typeparam>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class XmlDerivedTypeAttribute<T> : XmlDerivedTypeAttribute
    {

        #region Public Properties

        /// <inheritdoc />
        public override Type DerivedType => typeof(T);

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the attribute with the derived type name as the discriminator.
        /// </summary>
        public XmlDerivedTypeAttribute()
        {
            TypeDiscriminator = typeof(T).Name;
        }

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="typeDiscriminator">The element name or attribute value that selects this type.</param>
        public XmlDerivedTypeAttribute(string typeDiscriminator)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(typeDiscriminator);
            TypeDiscriminator = typeDiscriminator;
        }

        #endregion

    }

}
