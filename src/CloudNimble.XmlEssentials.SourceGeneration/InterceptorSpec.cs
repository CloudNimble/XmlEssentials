using System;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// One intercepted <c>XmlSerializer</c> call site.
    /// </summary>
    internal readonly struct InterceptorSpec : IEquatable<InterceptorSpec>
    {

        #region Public Properties

        /// <summary>
        /// Gets the interceptable location data.
        /// </summary>
        internal string Data { get; }

        /// <summary>
        /// Gets whether the call is Deserialize.
        /// </summary>
        internal bool IsDeserialize { get; }

        /// <summary>
        /// Gets the generated context type's fully qualified name.
        /// </summary>
        internal string ContextTypeName { get; }

        /// <summary>
        /// Gets the type-info property name on the context.
        /// </summary>
        internal string PropertyName { get; }

        /// <summary>
        /// Gets the fully qualified CLR type name of the value.
        /// </summary>
        internal string ValueTypeName { get; }

        /// <summary>
        /// Gets the interceptable location version.
        /// </summary>
        internal int Version { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the spec.
        /// </summary>
        /// <param name="version">The location version.</param>
        /// <param name="data">The location data.</param>
        /// <param name="isDeserialize">Whether the call is Deserialize.</param>
        /// <param name="valueTypeName">The value CLR type name.</param>
        /// <param name="contextTypeName">The context type name.</param>
        /// <param name="propertyName">The type-info property name.</param>
        internal InterceptorSpec(int version, string data, bool isDeserialize, string valueTypeName, string contextTypeName, string propertyName)
        {
            Version = version;
            Data = data;
            IsDeserialize = isDeserialize;
            ValueTypeName = valueTypeName;
            ContextTypeName = contextTypeName;
            PropertyName = propertyName;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public bool Equals(InterceptorSpec other)
        {
            return Version == other.Version
                && Data == other.Data
                && IsDeserialize == other.IsDeserialize
                && ValueTypeName == other.ValueTypeName
                && ContextTypeName == other.ContextTypeName
                && PropertyName == other.PropertyName;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is InterceptorSpec other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }

        #endregion

    }

}
