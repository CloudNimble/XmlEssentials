using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Provides metadata about a set of types that is relevant to XML serialization, and provides a base for source-generated contexts.
    /// </summary>
    /// <remarks>
    /// This class is analogous to <c>JsonSerializerContext</c> in <c>System.Text.Json</c>.
    /// </remarks>
    public abstract class XmlSerializerContext : IXmlTypeInfoResolver
    {

        #region Public Properties

        /// <summary>
        /// Gets the run-time specified options of the context.
        /// </summary>
        /// <value>
        /// The options bound to the context. If no options were passed when the context was created, a new instance is bound and returned.
        /// </value>
        /// <remarks>
        /// The <see cref="XmlSerializerOptions.TypeInfoResolver"/> of the options is this context.
        /// </remarks>
        public XmlSerializerOptions Options { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Called from constructors in derived classes to initialize the <see cref="XmlSerializerContext"/> class and bind it
        /// to the specified options.
        /// </summary>
        /// <param name="options">The run-time options to bind to the context, or <see langword="null"/> to bind a new instance with default settings.</param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="options"/> is read-only.
        /// </exception>
        /// <remarks>
        /// The context becomes the <see cref="XmlSerializerOptions.TypeInfoResolver"/> of the options.
        /// </remarks>
        protected XmlSerializerContext(XmlSerializerOptions? options)
        {
            Options = options ?? new XmlSerializerOptions();
            Options.TypeInfoResolver = this;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// When overridden in a derived class, gets metadata for the specified type.
        /// </summary>
        /// <param name="type">The type to fetch metadata about.</param>
        /// <returns>
        /// The metadata for <paramref name="type"/>, or <see langword="null"/> if the context has no metadata for the type.
        /// </returns>
        public abstract XmlTypeInfo? GetTypeInfo(Type type);

        /// <inheritdoc />
        /// <remarks>
        /// When other options ask, as they do when the context is one of several in
        /// <see cref="XmlSerializerOptions.TypeInfoResolverChain"/>, a type whose converter exists already (a built-in
        /// scalar, an XML value, <see cref="object"/>) is answered for the options that ask.
        /// </remarks>
        XmlTypeInfo? IXmlTypeInfoResolver.GetTypeInfo(Type type, XmlSerializerOptions options)
        {
            return ReferenceEquals(options, Options)
                ? GetTypeInfo(type)
                : XmlMetadataServices.GetBuiltInTypeInfo(type, options) ?? GetTypeInfo(type);
        }

        #endregion

    }

}
