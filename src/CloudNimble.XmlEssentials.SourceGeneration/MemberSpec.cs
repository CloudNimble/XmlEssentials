using System;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// A serializable member of an object type.
    /// </summary>
    internal readonly struct MemberSpec : IEquatable<MemberSpec>
    {

        #region Public Properties

        /// <summary>
        /// Gets whether the member can be assigned after construction.
        /// </summary>
        internal bool CanSet { get; }

        /// <summary>
        /// Gets the CLR member name.
        /// </summary>
        internal string ClrName { get; }

        /// <summary>
        /// Gets the fully qualified CLR type name.
        /// </summary>
        internal string ClrTypeName { get; }

        /// <summary>
        /// Gets whether the member is an XML attribute.
        /// </summary>
        internal bool IsAttribute { get; }

        /// <summary>
        /// Gets whether simple content is written as CDATA.
        /// </summary>
        internal bool IsCData { get; }

        /// <summary>
        /// Gets whether the member is a collection.
        /// </summary>
        internal bool IsCollection { get; }

        /// <summary>
        /// Gets whether the member is extension data.
        /// </summary>
        internal bool IsExtensionData { get; }

        /// <summary>
        /// Gets whether the member is mixed content.
        /// </summary>
        internal bool IsMixed { get; }

        /// <summary>
        /// Gets whether a null value is nillable.
        /// </summary>
        internal bool IsNillable { get; }

        /// <summary>
        /// Gets whether the member is required.
        /// </summary>
        internal bool IsRequired { get; }

        /// <summary>
        /// Gets whether the member is simple content.
        /// </summary>
        internal bool IsText { get; }

        /// <summary>
        /// Gets whether a collection is wrapped.
        /// </summary>
        internal bool IsWrapped { get; }

        /// <summary>
        /// Gets the item element local name for a collection.
        /// </summary>
        internal string ItemLocalName { get; }

        /// <summary>
        /// Gets the fully qualified item CLR type name for a collection.
        /// </summary>
        internal string ItemTypeName { get; }

        /// <summary>
        /// Gets the XML local name.
        /// </summary>
        internal string LocalName { get; }

        /// <summary>
        /// Gets the namespace URI, or empty.
        /// </summary>
        internal string NamespaceUri { get; }

        /// <summary>
        /// Gets the ignore condition value (matches <c>XmlIgnoreCondition</c>).
        /// </summary>
        internal int IgnoreCondition { get; }

        /// <summary>
        /// Gets the explicit order, or <c>-1</c>.
        /// </summary>
        internal int Order { get; }

        /// <summary>
        /// Gets the name of the constructor parameter this member fills, or null.
        /// </summary>
        internal string? ConstructorParameter { get; }

        /// <summary>
        /// Gets the fully qualified name of the type that declares the member, when the member is annotated as
        /// never reading null; otherwise null. A null it holds is refused when it is written under
        /// <c>RespectNullableAnnotations</c>.
        /// </summary>
        internal string? NullGetOwner { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the member spec.
        /// </summary>
        /// <param name="clrName">The CLR member name.</param>
        /// <param name="clrTypeName">The fully qualified CLR type name.</param>
        /// <param name="localName">The XML local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <param name="isAttribute">Whether the member is an attribute.</param>
        /// <param name="isText">Whether the member is simple content.</param>
        /// <param name="isCData">Whether simple content is CDATA.</param>
        /// <param name="isCollection">Whether the member is a collection.</param>
        /// <param name="isWrapped">Whether a collection is wrapped.</param>
        /// <param name="itemLocalName">The item element name.</param>
        /// <param name="itemTypeName">The item CLR type name.</param>
        /// <param name="isNillable">Whether null is nillable.</param>
        /// <param name="isRequired">Whether the member is required.</param>
        /// <param name="canSet">Whether the member can be set.</param>
        /// <param name="ignoreCondition">The ignore condition.</param>
        /// <param name="order">The explicit order.</param>
        /// <param name="isExtensionData">Whether the member is extension data.</param>
        /// <param name="isMixed">Whether the member is mixed content.</param>
        internal MemberSpec(
            string clrName,
            string clrTypeName,
            string localName,
            string namespaceUri,
            bool isAttribute,
            bool isText,
            bool isCData,
            bool isCollection,
            bool isWrapped,
            string itemLocalName,
            string itemTypeName,
            bool isNillable,
            bool isRequired,
            bool canSet,
            int ignoreCondition,
            int order,
            bool isExtensionData,
            bool isMixed)
        {
            ClrName = clrName;
            ClrTypeName = clrTypeName;
            LocalName = localName;
            NamespaceUri = namespaceUri;
            IsAttribute = isAttribute;
            IsText = isText;
            IsCData = isCData;
            IsCollection = isCollection;
            IsWrapped = isWrapped;
            ItemLocalName = itemLocalName;
            ItemTypeName = itemTypeName;
            IsNillable = isNillable;
            IsRequired = isRequired;
            CanSet = canSet;
            IgnoreCondition = ignoreCondition;
            Order = order;
            IsExtensionData = isExtensionData;
            IsMixed = isMixed;
        }

        /// <summary>
        /// Copies <paramref name="source"/> with what is known beyond how the member is written.
        /// </summary>
        /// <param name="source">The member as it was bound.</param>
        /// <param name="constructorParameter">The name of the constructor parameter the member fills, or null.</param>
        /// <param name="nullGetOwner">The declaring type, when the member never reads null; otherwise null.</param>
        internal MemberSpec(MemberSpec source, string? constructorParameter, string? nullGetOwner)
            : this(
                source.ClrName,
                source.ClrTypeName,
                source.LocalName,
                source.NamespaceUri,
                source.IsAttribute,
                source.IsText,
                source.IsCData,
                source.IsCollection,
                source.IsWrapped,
                source.ItemLocalName,
                source.ItemTypeName,
                source.IsNillable,
                source.IsRequired,
                source.CanSet,
                source.IgnoreCondition,
                source.Order,
                source.IsExtensionData,
                source.IsMixed)
        {
            ConstructorParameter = constructorParameter;
            NullGetOwner = nullGetOwner;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public bool Equals(MemberSpec other)
        {
            return ClrName == other.ClrName
                && ClrTypeName == other.ClrTypeName
                && LocalName == other.LocalName
                && NamespaceUri == other.NamespaceUri
                && IsAttribute == other.IsAttribute
                && IsText == other.IsText
                && IsCData == other.IsCData
                && IsCollection == other.IsCollection
                && IsWrapped == other.IsWrapped
                && ItemLocalName == other.ItemLocalName
                && ItemTypeName == other.ItemTypeName
                && IsNillable == other.IsNillable
                && IsRequired == other.IsRequired
                && CanSet == other.CanSet
                && IgnoreCondition == other.IgnoreCondition
                && Order == other.Order
                && IsExtensionData == other.IsExtensionData
                && IsMixed == other.IsMixed
                && ConstructorParameter == other.ConstructorParameter
                && NullGetOwner == other.NullGetOwner;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is MemberSpec other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ClrName.GetHashCode() ^ LocalName.GetHashCode();
        }

        #endregion

    }

}
