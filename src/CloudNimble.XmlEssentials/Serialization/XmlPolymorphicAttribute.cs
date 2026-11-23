using System;
using CloudNimble.Text.Xml;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Indicates, when placed on a type, that the type is serialized polymorphically, and specifies how its type discriminator
    /// is written. This class cannot be inherited.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class XmlPolymorphicAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets where the type discriminator is written.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies where the type discriminator is written. The default is
        /// <see cref="XmlDiscriminatorPlacement.ElementName"/>.
        /// </value>
        public XmlDiscriminatorPlacement DiscriminatorPlacement { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether closed derived types are inferred when the type has no
        /// <see cref="XmlDerivedTypeAttribute"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if closed derived types are inferred; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// The serializer does not read this property.
        /// </remarks>
        public bool InferClosedTypePolymorphism { get; set; }

        /// <summary>
        /// Gets or sets the local name of the attribute that holds the type discriminator.
        /// </summary>
        /// <value>
        /// The local name of the discriminator attribute. The default is <see langword="null"/>.
        /// </value>
        /// <remarks>
        /// The name is required when <see cref="DiscriminatorPlacement"/> is <see cref="XmlDiscriminatorPlacement.Attribute"/>:
        /// the reflection-based serializer throws a <see cref="NotSupportedException"/> for the annotated type if the value is
        /// <see langword="null"/>, empty, or white space. The name is not used for the other placements.
        /// </remarks>
        public string? TypeDiscriminatorPropertyName { get; set; }

        /// <summary>
        /// Gets or sets the behavior when serializing or deserializing a derived type that is not declared.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies how an undeclared derived type is handled. The default is
        /// <see cref="XmlUnknownDerivedTypeHandling.FailSerialization"/>.
        /// </value>
        public XmlUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get; set; }

        #endregion

    }

}
