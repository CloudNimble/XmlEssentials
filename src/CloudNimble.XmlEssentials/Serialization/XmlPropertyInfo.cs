using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Provides XML serialization-related metadata about a property or field. This class cannot be inherited.
    /// </summary>
    public sealed class XmlPropertyInfo
    {

        #region Public Properties

        /// <summary>
        /// Gets a value that indicates whether the member is serialized as an XML attribute.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the member is serialized as an XML attribute; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsAttribute { get; }

        /// <summary>
        /// Gets a value that indicates whether the member is nillable.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the member is nillable; otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// A member is nillable when its type is a reference type, a <see cref="Nullable{T}"/>, or a collection, or when
        /// <see cref="XmlElementAttribute.IsNullable"/> is <see langword="true"/>. How a <see langword="null"/> value is written
        /// depends on <see cref="XmlSerializerOptions.NilHandling"/> and on the ignore condition of the member.
        /// </remarks>
        public bool IsNillable { get; }

        /// <summary>
        /// Gets a value that indicates whether the member must be present on deserialization.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the member must be present on deserialization; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsRequired { get; }

        /// <summary>
        /// Gets a value that indicates whether the member is serialized as the text content of the element for its containing type.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the member is serialized as text content; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsText { get; }

        /// <summary>
        /// Gets the XML name of the member.
        /// </summary>
        /// <value>
        /// The name of the element or attribute that the member is serialized as.
        /// </value>
        public XmlName Name { get; }

        /// <summary>
        /// Gets the serialization order of the member among the members of its containing type.
        /// </summary>
        /// <value>
        /// The order of the member, or <c>-1</c> if no order is set.
        /// </value>
        public int Order { get; }

        /// <summary>
        /// Gets the metadata for the type of the member.
        /// </summary>
        /// <value>
        /// The serialization metadata for the type of the member.
        /// </value>
        public XmlTypeInfo PropertyType { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates property metadata.
        /// </summary>
        /// <param name="name">The XML name.</param>
        /// <param name="propertyType">The member type info.</param>
        /// <param name="isNillable">Whether null is representable.</param>
        /// <param name="isRequired">Whether the member must be present when deserializing.</param>
        /// <param name="order">The order, or <c>-1</c> if unset.</param>
        /// <param name="isAttribute">Whether the member is an attribute.</param>
        /// <param name="isText">Whether the member is simple content.</param>
        internal XmlPropertyInfo(XmlName name, XmlTypeInfo propertyType, bool isNillable, bool isRequired, int order, bool isAttribute, bool isText)
        {
            Name = name;
            PropertyType = propertyType;
            IsNillable = isNillable;
            IsRequired = isRequired;
            Order = order;
            IsAttribute = isAttribute;
            IsText = isText;
        }

        #endregion

    }

}
