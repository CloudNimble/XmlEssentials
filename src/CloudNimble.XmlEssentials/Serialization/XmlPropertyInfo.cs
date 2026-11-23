using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Metadata for one serialized member of an object.
    /// </summary>
    public sealed class XmlPropertyInfo
    {

        #region Public Properties

        /// <summary>
        /// Gets whether the member is mapped as an XML attribute.
        /// </summary>
        public bool IsAttribute { get; }

        /// <summary>
        /// Gets whether a null value is written as a nillable element.
        /// </summary>
        public bool IsNillable { get; }

        /// <summary>
        /// Gets whether the member must be present when deserializing.
        /// </summary>
        public bool IsRequired { get; }

        /// <summary>
        /// Gets whether the member is mapped as simple content of the parent element.
        /// </summary>
        public bool IsText { get; }

        /// <summary>
        /// Gets the XML name of the member.
        /// </summary>
        public XmlName Name { get; }

        /// <summary>
        /// Gets the order among sibling members. <c>-1</c> means unset.
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Gets the type info of the member's CLR type.
        /// </summary>
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
