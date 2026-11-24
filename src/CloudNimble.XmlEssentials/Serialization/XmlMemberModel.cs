using System;
using System.Reflection;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// A serializable member before typed accessors are bound.
    /// </summary>
    internal readonly struct XmlMemberModel
    {

        #region Public Properties

        /// <summary>
        /// Gets whether the member can be set after construction.
        /// </summary>
        internal bool CanSet { get; }

        /// <summary>
        /// Gets the CLR type used to convert the member, which may be a typed element choice.
        /// </summary>
        internal Type ConverterType { get; }

        /// <summary>
        /// Gets the discovery index, used when order is unset.
        /// </summary>
        internal int DeclarationIndex { get; }

        /// <summary>
        /// Gets when the member is omitted on write.
        /// </summary>
        internal XmlIgnoreCondition Ignore { get; }

        /// <summary>
        /// Gets whether the member is mapped as an XML attribute.
        /// </summary>
        internal bool IsAttribute { get; }

        /// <summary>
        /// Gets whether the member is a collection of items.
        /// </summary>
        internal bool IsCollection { get; }

        /// <summary>
        /// Gets whether the member is written as a CDATA section.
        /// </summary>
        internal bool IsCData { get; }

        /// <summary>
        /// Gets whether a null value is nillable.
        /// </summary>
        internal bool IsNillable { get; }

        /// <summary>
        /// Gets the item element local name for a collection.
        /// </summary>
        internal string ItemLocalName { get; }

        /// <summary>
        /// Gets whether the member is the simple content of the parent element.
        /// </summary>
        internal bool IsText { get; }

        /// <summary>
        /// Gets whether a collection is written with a wrapper element.
        /// </summary>
        internal bool IsWrapped { get; }

        /// <summary>
        /// Gets whether the member must appear when deserializing.
        /// </summary>
        internal bool IsRequired { get; }

        /// <summary>
        /// Gets the XML local name.
        /// </summary>
        internal string LocalName { get; }

        /// <summary>
        /// Gets the member.
        /// </summary>
        internal XmlMemberFacts Member { get; }

        /// <summary>
        /// Gets the namespace URI, or empty when the member inherits the parent's namespace.
        /// </summary>
        internal string NamespaceUri { get; }

        /// <summary>
        /// Gets the explicit order, or <c>-1</c>.
        /// </summary>
        internal int Order { get; }

        /// <summary>
        /// Gets the member CLR type.
        /// </summary>
        internal Type Type { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a member model.
        /// </summary>
        /// <param name="member">The property or field.</param>
        /// <param name="type">The member CLR type.</param>
        /// <param name="localName">The XML local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <param name="order">The explicit order, or <c>-1</c>.</param>
        /// <param name="declarationIndex">The discovery index.</param>
        /// <param name="isRequired">Whether the member must appear when deserializing.</param>
        /// <param name="isNillable">Whether null is nillable.</param>
        /// <param name="ignore">When the member is omitted on write.</param>
        /// <param name="canSet">Whether the member can be set after construction.</param>
        /// <param name="isAttribute">Whether the member is an XML attribute.</param>
        /// <param name="isText">Whether the member is simple content.</param>
        /// <param name="isCData">Whether simple content is written as CDATA.</param>
        /// <param name="isCollection">Whether the member is a collection.</param>
        /// <param name="isWrapped">Whether a collection is wrapped.</param>
        /// <param name="itemLocalName">The item element name for a collection.</param>
        /// <param name="converterType">The converter CLR type, or null to use <paramref name="type"/>.</param>
        internal XmlMemberModel(XmlMemberFacts member, Type type, string localName, string namespaceUri, int order, int declarationIndex, bool isRequired, bool isNillable, XmlIgnoreCondition ignore, bool canSet, bool isAttribute, bool isText, bool isCData, bool isCollection = false, bool isWrapped = false, string itemLocalName = "", Type? converterType = null)
        {
            Member = member;
            Type = type;
            ConverterType = converterType ?? type;
            LocalName = localName;
            NamespaceUri = namespaceUri;
            Order = order;
            DeclarationIndex = declarationIndex;
            IsRequired = isRequired;
            IsNillable = isNillable;
            Ignore = ignore;
            CanSet = canSet;
            IsAttribute = isAttribute;
            IsText = isText;
            IsCData = isCData;
            IsCollection = isCollection;
            IsWrapped = isWrapped;
            ItemLocalName = itemLocalName;
        }

        #endregion

    }

}
