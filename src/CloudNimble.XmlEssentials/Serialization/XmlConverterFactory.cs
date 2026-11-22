using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Creates converters for a family of types, such as every enum or every closed form of a generic type.
    /// </summary>
    public abstract class XmlConverterFactory : XmlConverter
    {

        #region Public Methods

        /// <summary>
        /// Creates a converter for <paramref name="typeToConvert"/>.
        /// </summary>
        /// <param name="typeToConvert">The CLR type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The converter. It must be an <see cref="XmlConverter{T}"/> for exactly <paramref name="typeToConvert"/>.</returns>
        public abstract XmlConverter? CreateConverter(Type typeToConvert, XmlSerializerOptions options);

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal sealed override XmlTypeInfo? CreateTypeInfo(Type typeToConvert, XmlSerializerOptions options)
        {
            return null;
        }

        /// <inheritdoc />
        internal sealed override XmlConverter Resolve(Type typeToConvert, XmlSerializerOptions options)
        {
            return CreateConverter(typeToConvert, options)
                ?? throw new InvalidOperationException($"Converter factory '{GetType()}' returned null for '{typeToConvert}'.");
        }

        #endregion

    }

}
