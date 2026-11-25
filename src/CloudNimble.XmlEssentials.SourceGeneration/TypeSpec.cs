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
        /// Gets whether the write fast path is emitted for the type.
        /// </summary>
        internal bool EmitSerialization { get; }

        /// <summary>
        /// Gets the statements that create the type info of an object type from metadata, or empty for any other type.
        /// </summary>
        internal string Metadata { get; }

        /// <summary>
        /// Gets the declarations of the accessors <see cref="Metadata"/> calls, or empty.
        /// </summary>
        internal string Accessors { get; }

        /// <summary>
        /// Gets whether the write fast path can write the type: it is an object type with nothing the fast path
        /// leaves to the runtime library.
        /// </summary>
        internal bool FastPath { get; }

        /// <summary>
        /// Gets the generation mode of the type: 0 default, 1 metadata, 2 serialization, 3 both.
        /// </summary>
        internal int Mode { get; }

        /// <summary>
        /// Gets whether the type info is created from metadata, which every mode but serialization-only does.
        /// </summary>
        internal bool EmitMetadata => Metadata.Length > 0 && Mode != 2;

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

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the type spec.
        /// </summary>
        /// <param name="clrTypeName">The fully qualified CLR type name.</param>
        /// <param name="propertyName">The type-info property name.</param>
        /// <param name="kind">The object kind.</param>
        /// <param name="isValueType">Whether the type is a value type.</param>
        /// <param name="isNillable">Whether null is nillable.</param>
        /// <param name="emitSerialization">Whether the write fast path is emitted.</param>
        /// <param name="members">The members.</param>
        /// <param name="factory">The expression that creates the type info without reflection, or empty.</param>
        /// <param name="metadata">The statements that create the type info from metadata, or empty.</param>
        /// <param name="accessors">The declarations of the accessors the metadata calls, or empty.</param>
        /// <param name="fastPath">Whether the write fast path can write the type.</param>
        /// <param name="mode">The generation mode of the type.</param>
        internal TypeSpec(
            string clrTypeName,
            string propertyName,
            int kind,
            bool isValueType,
            bool isNillable,
            bool emitSerialization,
            EquatableArray<MemberSpec> members,
            string factory = "",
            string metadata = "",
            string accessors = "",
            bool fastPath = false,
            int mode = 0)
        {
            ClrTypeName = clrTypeName;
            PropertyName = propertyName;
            Kind = kind;
            IsValueType = isValueType;
            IsNillable = isNillable;
            EmitSerialization = emitSerialization;
            Members = members;
            Factory = factory;
            Metadata = metadata;
            Accessors = accessors;
            FastPath = fastPath;
            Mode = mode;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public bool Equals(TypeSpec other)
        {
            return ClrTypeName == other.ClrTypeName
                && PropertyName == other.PropertyName
                && Kind == other.Kind
                && IsValueType == other.IsValueType
                && IsNillable == other.IsNillable
                && EmitSerialization == other.EmitSerialization
                && Members.Equals(other.Members)
                && Factory == other.Factory
                && Metadata == other.Metadata
                && Accessors == other.Accessors
                && FastPath == other.FastPath
                && Mode == other.Mode;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is TypeSpec other && Equals(other);
        }

        /// <summary>
        /// Returns the same type spec under another type-info property name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The renamed spec.</returns>
        internal TypeSpec WithPropertyName(string propertyName)
        {
            return new TypeSpec(ClrTypeName, propertyName, Kind, IsValueType, IsNillable, EmitSerialization, Members, Factory, Metadata, Accessors, FastPath, Mode);
        }

        /// <summary>
        /// Returns the same type spec under another generation mode.
        /// </summary>
        /// <param name="mode">The mode: 0 default, 1 metadata, 2 serialization, 3 both.</param>
        /// <returns>The spec, with the write fast path on or off as the mode says.</returns>
        internal TypeSpec WithMode(int mode)
        {
            return new TypeSpec(ClrTypeName, PropertyName, Kind, IsValueType, IsNillable, FastPath && mode != 1, Members, Factory, Metadata, Accessors, FastPath, mode);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ClrTypeName.GetHashCode();
        }

        #endregion

    }

}
