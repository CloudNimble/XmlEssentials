using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Indicates, when placed on a type declaration, that the specified derived type is opted into polymorphic serialization.
    /// </summary>
    /// <remarks>
    /// Use <see cref="XmlDerivedTypeAttribute{T}"/> to specify the derived type.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public abstract class XmlDerivedTypeAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// When overridden in a derived class, gets the derived type that is supported in polymorphic serialization of the declared base type.
        /// </summary>
        /// <value>
        /// The derived type.
        /// </value>
        public abstract Type DerivedType { get; }

        /// <summary>
        /// Gets or sets the namespace URI of the element that identifies the derived type.
        /// </summary>
        /// <value>
        /// The namespace URI. The default is <see langword="null"/>.
        /// </value>
        /// <remarks>
        /// The serializer does not read this property.
        /// </remarks>
        public string? Namespace { get; set; }

        /// <summary>
        /// Gets or sets the type discriminator identifier that is used for the serialization of the derived type.
        /// </summary>
        /// <value>
        /// The type discriminator. Depending on <see cref="XmlPolymorphicAttribute.DiscriminatorPlacement"/>, it is the local name of
        /// the element or the value of the discriminator attribute. If the value is not a string, the name of
        /// <see cref="DerivedType"/> is used.
        /// </value>
        /// <remarks>
        /// When the discriminator is an element name, the element naming policy is applied to it.
        /// </remarks>
        public object? TypeDiscriminator { get; protected set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Called from constructors in derived classes to initialize the <see cref="XmlDerivedTypeAttribute"/> class.
        /// </summary>
        public XmlDerivedTypeAttribute()
        {
        }

        #endregion

    }

}
