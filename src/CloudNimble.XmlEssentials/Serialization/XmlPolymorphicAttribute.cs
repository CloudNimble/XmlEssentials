using System;
using CloudNimble.Text.Xml;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Declares that a type is serialized with a derived-type discriminator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class XmlPolymorphicAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets where the discriminator is written.
        /// </summary>
        public XmlDiscriminatorPlacement DiscriminatorPlacement { get; set; }

        /// <summary>
        /// Gets or sets whether closed derived types are inferred when no <see cref="XmlDerivedTypeAttribute"/> is present.
        /// </summary>
        public bool InferClosedTypePolymorphism { get; set; }

        /// <summary>
        /// Gets or sets the discriminator attribute or element name when placement is not the element name itself.
        /// </summary>
        public string? TypeDiscriminatorPropertyName { get; set; }

        /// <summary>
        /// Gets or sets how unknown derived types are handled.
        /// </summary>
        public XmlUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get; set; }

        #endregion

    }

}
