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
        /// <summary>
        /// The named arguments passed for constructor parameters whose members are ignored, each with a leading comma, or empty.
        /// </summary>
        internal string ConstructorTail { get; }

        /// <summary>
        /// Gets the name of a member the reflection resolver would populate and the generated converter cannot, or
        /// empty. The converter refuses <c>PreferredObjectCreationHandling.Populate</c> when it is not empty.
        /// </summary>
        internal string PopulateRefusal { get; }

        internal bool EmitSerialization { get; }

        /// <summary>
        /// The expression that creates the type info without reflection, for a collection, a dictionary, an
        /// enumeration, or a nullable value the runtime library reads and writes; otherwise empty.
        /// </summary>
        internal string Factory { get; }

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
        /// <param name="constructorTail">The named arguments for constructor parameters whose members are ignored, or empty.</param>
        /// <param name="factory">The expression that creates the type info without reflection, or empty.</param>
        /// <param name="populateRefusal">The name of a member the generated converter cannot populate, or empty.</param>
        internal TypeSpec(
            string clrTypeName,
            string propertyName,
            string rootName,
            string rootNamespace,
            int kind,
            bool isValueType,
            bool isNillable,
            bool emitSerialization,
            EquatableArray<MemberSpec> members,
            string constructorTail,
            string factory = "",
            string populateRefusal = "")
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
            ConstructorTail = constructorTail;
            Factory = factory;
            PopulateRefusal = populateRefusal;
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
                && Members.Equals(other.Members)
                && ConstructorTail == other.ConstructorTail
                && Factory == other.Factory
                && PopulateRefusal == other.PopulateRefusal;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is TypeSpec other && Equals(other);
        }

        /// <summary>
        /// Returns the same type spec under another type-info property name.
        /// </summary>
        /// <param name="propertyName">The property name, which also names the generated converter.</param>
        /// <returns>The renamed spec.</returns>
        internal TypeSpec WithPropertyName(string propertyName)
        {
            return new TypeSpec(ClrTypeName, propertyName, RootName, RootNamespace, Kind, IsValueType, IsNillable, EmitSerialization, Members, ConstructorTail, Factory, PopulateRefusal);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ClrTypeName.GetHashCode();
        }

        #endregion

    }

}
