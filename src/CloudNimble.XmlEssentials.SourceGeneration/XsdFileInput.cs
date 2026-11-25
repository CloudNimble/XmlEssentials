using System;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// One <c>*.xsd</c> AdditionalFile, with optional MSBuild metadata.
    /// </summary>
    internal readonly struct XsdFileInput : IEquatable<XsdFileInput>
    {

        #region Public Properties

        /// <summary>
        /// Gets the context type name, or empty to derive it from the file name.
        /// </summary>
        internal string ContextName { get; }

        /// <summary>
        /// Gets the C# namespace, or empty to use the compilation default.
        /// </summary>
        internal string NamespaceName { get; }

        /// <summary>
        /// Gets the AdditionalFile path.
        /// </summary>
        internal string Path { get; }

        /// <summary>
        /// Gets the schema text.
        /// </summary>
        internal string Text { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the input.
        /// </summary>
        /// <param name="path">The AdditionalFile path.</param>
        /// <param name="text">The schema text.</param>
        /// <param name="namespaceName">The C# namespace metadata, or empty.</param>
        /// <param name="contextName">The context type name metadata, or empty.</param>
        internal XsdFileInput(string path, string text, string namespaceName, string contextName)
        {
            Path = path;
            Text = text;
            NamespaceName = namespaceName;
            ContextName = contextName;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public bool Equals(XsdFileInput other)
        {
            return Path == other.Path
                && Text == other.Text
                && NamespaceName == other.NamespaceName
                && ContextName == other.ContextName;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is XsdFileInput other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }

        #endregion

    }

}
