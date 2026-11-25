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
        /// Gets the generated context type's fully qualified name.
        /// </summary>
        internal string ContextTypeName { get; }

        /// <summary>
        /// Gets the interceptable location data.
        /// </summary>
        internal string Data { get; }

        /// <summary>
        /// Gets the modifier of the first parameter of the intercepted method: <c>ref </c>, or an empty string.
        /// </summary>
        internal string InputModifier { get; }

        /// <summary>
        /// Gets the fully qualified type of the first parameter of the intercepted method: the value for
        /// <c>Serialize</c>, the input for <c>Deserialize</c>.
        /// </summary>
        internal string InputTypeName { get; }

        /// <summary>
        /// Gets whether the call is Deserialize.
        /// </summary>
        internal bool IsDeserialize { get; }

        /// <summary>
        /// Gets the type-info property name on the context.
        /// </summary>
        internal string PropertyName { get; }

        /// <summary>
        /// Gets the fully qualified return type of the intercepted method, with its nullable annotation.
        /// </summary>
        internal string ReturnTypeName { get; }

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
        /// Creates the spec of a call site that no context has been matched to yet.
        /// </summary>
        /// <param name="version">The location version.</param>
        /// <param name="data">The location data.</param>
        /// <param name="isDeserialize">Whether the call is Deserialize.</param>
        /// <param name="valueTypeName">The value CLR type name.</param>
        /// <param name="inputModifier">The modifier of the first parameter.</param>
        /// <param name="inputTypeName">The type of the first parameter.</param>
        /// <param name="returnTypeName">The return type.</param>
        internal InterceptorSpec(int version, string data, bool isDeserialize, string valueTypeName, string inputModifier, string inputTypeName, string returnTypeName)
            : this(version, data, isDeserialize, valueTypeName, inputModifier, inputTypeName, returnTypeName, "", "")
        {
        }

        /// <summary>
        /// Creates the spec.
        /// </summary>
        /// <param name="version">The location version.</param>
        /// <param name="data">The location data.</param>
        /// <param name="isDeserialize">Whether the call is Deserialize.</param>
        /// <param name="valueTypeName">The value CLR type name.</param>
        /// <param name="inputModifier">The modifier of the first parameter.</param>
        /// <param name="inputTypeName">The type of the first parameter.</param>
        /// <param name="returnTypeName">The return type.</param>
        /// <param name="contextTypeName">The context type name.</param>
        /// <param name="propertyName">The type-info property name.</param>
        internal InterceptorSpec(int version, string data, bool isDeserialize, string valueTypeName, string inputModifier, string inputTypeName, string returnTypeName, string contextTypeName, string propertyName)
        {
            Version = version;
            Data = data;
            IsDeserialize = isDeserialize;
            ValueTypeName = valueTypeName;
            InputModifier = inputModifier;
            InputTypeName = inputTypeName;
            ReturnTypeName = returnTypeName;
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
                && InputModifier == other.InputModifier
                && InputTypeName == other.InputTypeName
                && ReturnTypeName == other.ReturnTypeName
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

        /// <summary>
        /// Returns this call site matched to a context.
        /// </summary>
        /// <param name="contextTypeName">The context type name.</param>
        /// <param name="propertyName">The type-info property name.</param>
        /// <returns>The spec.</returns>
        internal InterceptorSpec WithContext(string contextTypeName, string propertyName)
        {
            return new InterceptorSpec(Version, Data, IsDeserialize, ValueTypeName, InputModifier, InputTypeName, ReturnTypeName, contextTypeName, propertyName);
        }

        #endregion

    }

}
