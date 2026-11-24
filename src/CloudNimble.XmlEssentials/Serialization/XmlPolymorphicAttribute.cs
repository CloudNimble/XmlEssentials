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
        /// Gets or sets a value that indicates whether the derived types of a closed type are inferred.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the derived types of the type are taken from its closed hierarchy;
        /// <see langword="false"/> if they are not, whatever the options say. When the property is not set,
        /// <see cref="XmlSerializerOptions.InferClosedTypePolymorphism"/> decides. The getter returns
        /// <see langword="false"/> for a property that is not set.
        /// </value>
        /// <remarks>
        /// <para>
        /// A closed type is a class declared with the <see langword="closed"/> modifier, which needs C# 15 and
        /// .NET 11. Its derived types are all in its own assembly, and the compiler lists them. With this
        /// property, a derived type is written and read as if the type had an
        /// <see cref="XmlDerivedTypeAttribute"/> for it, with the name of the derived type as its discriminator.
        /// </para>
        /// <para>
        /// Derived types that are declared with <see cref="XmlDerivedTypeAttribute"/> are the whole list: nothing
        /// is inferred for a type that declares one.
        /// </para>
        /// <para>
        /// Setting the property to <see langword="true"/> on a type that is not closed throws an
        /// <see cref="InvalidOperationException"/> the first time the type is used.
        /// </para>
        /// </remarks>
        public bool InferClosedTypePolymorphism
        {
            get => InferClosedTypePolymorphismOrNull ?? false;
            set => InferClosedTypePolymorphismOrNull = value;
        }

        /// <summary>
        /// Gets the value <see cref="InferClosedTypePolymorphism"/> was set to, or null when it was not set.
        /// </summary>
        internal bool? InferClosedTypePolymorphismOrNull { get; private set; }

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
