using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Base class for source-generated serialization metadata. Analogous to <c>JsonSerializerContext</c>.
    /// </summary>
    public abstract class XmlSerializerContext : IXmlTypeInfoResolver
    {

        #region Public Properties

        /// <summary>
        /// Gets the options bound to this context. Their <see cref="XmlSerializerOptions.TypeInfoResolver"/> is this context.
        /// </summary>
        public XmlSerializerOptions Options { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a context and makes it the <see cref="XmlSerializerOptions.TypeInfoResolver"/> of its options.
        /// </summary>
        /// <param name="options">The options to bind, or <see langword="null"/> for new default options.</param>
        /// <exception cref="InvalidOperationException"><paramref name="options"/> is read-only.</exception>
        protected XmlSerializerContext(XmlSerializerOptions? options)
        {
            Options = options ?? new XmlSerializerOptions();
            Options.TypeInfoResolver = this;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns type info for <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>The type info, or <see langword="null"/> when this context does not know the type.</returns>
        public abstract XmlTypeInfo? GetTypeInfo(Type type);

        /// <inheritdoc />
        XmlTypeInfo? IXmlTypeInfoResolver.GetTypeInfo(Type type, XmlSerializerOptions options)
        {
            return GetTypeInfo(type);
        }

        #endregion

    }

}
