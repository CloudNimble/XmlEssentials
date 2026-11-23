using System;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// A source-generated <c>XmlSerializerContext</c>.
    /// </summary>
    internal readonly struct ContextSpec : IEquatable<ContextSpec>
    {

        #region Public Properties

        /// <summary>
        /// Gets the accessibility keyword (<c>public</c> or <c>internal</c>).
        /// </summary>
        internal string Accessibility { get; }

        /// <summary>
        /// Gets the hint name for the generated file.
        /// </summary>
        internal string HintName { get; }

        /// <summary>
        /// Gets the fully qualified context type name.
        /// </summary>
        internal string ClrTypeName { get; }

        /// <summary>
        /// Gets the generation mode value (matches <c>XmlSourceGenerationMode</c>).
        /// </summary>
        internal int GenerationMode { get; }

        /// <summary>
        /// Gets the simple type name.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Gets the namespace, or empty for the global namespace.
        /// </summary>
        internal string Namespace { get; }

        /// <summary>
        /// Gets the options seed from <c>[XmlSourceGenerationOptions]</c>.
        /// </summary>
        internal OptionsSpec Options { get; }

        /// <summary>
        /// Gets the types in the closure.
        /// </summary>
        internal EquatableArray<TypeSpec> Types { get; }

        /// <summary>
        /// Gets whether the generated class should declare <c>: XmlSerializerContext</c>
        /// (XSD-generated contexts have no user partial).
        /// </summary>
        internal bool EmitInheritance { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the context spec.
        /// </summary>
        /// <param name="clrTypeName">The fully qualified context type name.</param>
        /// <param name="name">The simple type name.</param>
        /// <param name="namespaceName">The namespace.</param>
        /// <param name="accessibility">The accessibility keyword.</param>
        /// <param name="generationMode">The generation mode.</param>
        /// <param name="options">The options seed.</param>
        /// <param name="types">The types.</param>
        /// <param name="emitInheritance">Whether to emit the base clause.</param>
        internal ContextSpec(
            string clrTypeName,
            string name,
            string namespaceName,
            string accessibility,
            int generationMode,
            OptionsSpec options,
            EquatableArray<TypeSpec> types,
            bool emitInheritance = false)
        {
            ClrTypeName = clrTypeName;
            Name = name;
            Namespace = namespaceName;
            Accessibility = accessibility;
            GenerationMode = generationMode;
            Options = options;
            Types = types;
            EmitInheritance = emitInheritance;
            HintName = (string.IsNullOrEmpty(namespaceName) ? name : namespaceName + "." + name) + ".g.cs";
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public bool Equals(ContextSpec other)
        {
            return ClrTypeName == other.ClrTypeName
                && Name == other.Name
                && Namespace == other.Namespace
                && Accessibility == other.Accessibility
                && GenerationMode == other.GenerationMode
                && Options.Equals(other.Options)
                && Types.Equals(other.Types)
                && EmitInheritance == other.EmitInheritance;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is ContextSpec other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ClrTypeName.GetHashCode();
        }

        #endregion

    }

}
