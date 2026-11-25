using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Indicates, when placed on a type declaration, that <typeparamref name="T"/> is opted into polymorphic serialization
    /// and can appear where the annotated type is expected. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">
    /// The derived type that is supported in polymorphic serialization of the annotated type.
    /// </typeparam>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class XmlDerivedTypeAttribute<T> : XmlDerivedTypeAttribute
    {

        #region Public Properties

        /// <inheritdoc />
        public override Type DerivedType => typeof(T);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlDerivedTypeAttribute{T}"/> class that uses the name of <typeparamref name="T"/> as the type discriminator.
        /// </summary>
        public XmlDerivedTypeAttribute()
        {
            TypeDiscriminator = typeof(T).Name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlDerivedTypeAttribute{T}"/> class with the specified type discriminator.
        /// </summary>
        /// <param name="typeDiscriminator">The element name or attribute value that identifies <typeparamref name="T"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="typeDiscriminator"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="typeDiscriminator"/> is empty or consists only of white-space characters.
        /// </exception>
        public XmlDerivedTypeAttribute(string typeDiscriminator)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(typeDiscriminator);
            TypeDiscriminator = typeDiscriminator;
        }

        #endregion

    }

}
