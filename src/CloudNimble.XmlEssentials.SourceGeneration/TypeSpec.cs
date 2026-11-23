using System;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// A type in a generated context's closure.
    /// </summary>
    internal readonly struct TypeSpec : IEquatable<TypeSpec>
    {

        #region Public Properties

        /// <summary>
        /// Gets the fully qualified CLR type name.
        /// </summary>
        internal string ClrTypeName { get; }

        /// <summary>
        /// Gets whether serialization converters should be emitted.
        /// </summary>
        internal bool EmitSerialization { get; }

        /// <summary>
        /// Gets whether the type is a value type.
        /// </summary>
        internal bool IsValueType { get; }

        /// <summary>
        /// Gets whether a null value is nillable.
        /// </summary>
        internal bool IsNillable { get; }

        /// <summary>
        /// Gets the object kind: 0 object, 1 primitive, 2 collection, 3 enum.
        /// </summary>
        internal int Kind { get; }

        /// <summary>
        /// Gets the members in write order.
        /// </summary>
        internal EquatableArray<MemberSpec> Members { get; }

        /// <summary>
        /// Gets the generated <c>XmlTypeInfo&lt;T&gt;</c> property name.
        /// </summary>
        internal string PropertyName { get; }

        /// <summary>
        /// Gets the document element local name.
        /// </summary>
        internal string RootName { get; }

        /// <summary>
        /// Gets the document element namespace URI, or empty.
        /// </summary>
        internal string RootNamespace { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the type spec.
        /// </summary>
        /// <param name="clrTypeName">The fully qualified CLR type name.</param>
        /// <param name="propertyName">The type-info property name.</param>
        /// <param name="rootName">The document element name.</param>
        /// <param name="rootNamespace">The document element namespace.</param>
        /// <param name="kind">The object kind.</param>
        /// <param name="isValueType">Whether the type is a value type.</param>
        /// <param name="isNillable">Whether null is nillable.</param>
        /// <param name="emitSerialization">Whether a converter should be emitted.</param>
        /// <param name="members">The members.</param>
        internal TypeSpec(
            string clrTypeName,
            string propertyName,
            string rootName,
            string rootNamespace,
            int kind,
            bool isValueType,
            bool isNillable,
            bool emitSerialization,
            EquatableArray<MemberSpec> members)
        {
            ClrTypeName = clrTypeName;
            PropertyName = propertyName;
            RootName = rootName;
            RootNamespace = rootNamespace;
            Kind = kind;
            IsValueType = isValueType;
            IsNillable = isNillable;
            EmitSerialization = emitSerialization;
            Members = members;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public bool Equals(TypeSpec other)
        {
            return ClrTypeName == other.ClrTypeName
                && PropertyName == other.PropertyName
                && RootName == other.RootName
                && RootNamespace == other.RootNamespace
                && Kind == other.Kind
                && IsValueType == other.IsValueType
                && IsNillable == other.IsNillable
                && EmitSerialization == other.EmitSerialization
                && Members.Equals(other.Members);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is TypeSpec other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ClrTypeName.GetHashCode();
        }

        #endregion

    }

}
