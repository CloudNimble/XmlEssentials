using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Resolves type info from the converters of an <see cref="XmlSerializerOptions"/>: its
    /// <see cref="XmlSerializerOptions.Converters"/>, <see cref="XmlConverterAttribute"/> on the type, and the built-in converters.
    /// </summary>
    public sealed class DefaultXmlTypeInfoResolver : IXmlTypeInfoResolver
    {

        #region Public Properties

        /// <summary>
        /// Gets the shared resolver.
        /// </summary>
        public static DefaultXmlTypeInfoResolver Instance { get; } = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the resolver.
        /// </summary>
        internal DefaultXmlTypeInfoResolver()
        {
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">The converter found for <paramref name="type"/> does not convert exactly that type.</exception>
        public XmlTypeInfo? GetTypeInfo(Type type, XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(options);
            if (options.GetConverterOrNull(type) is not { } converter)
            {
                return null;
            }

            return converter.CreateTypeInfo(type, options)
                ?? throw new InvalidOperationException($"The converter '{converter.GetType()}' is not compatible with the type '{type}'.");
        }

        #endregion

    }

}
