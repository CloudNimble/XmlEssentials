using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Supports converting several types by using a factory pattern.
    /// </summary>
    /// <remarks>
    /// Use a factory for a family of types, such as every enumeration or every closed form of a generic type.
    /// </remarks>
    public abstract class XmlConverterFactory : XmlConverter
    {

        #region Public Methods

        /// <summary>
        /// When overridden in a derived class, creates a converter for the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type handled by the converter.</param>
        /// <param name="options">The serialization options to use.</param>
        /// <returns>
        /// A converter that derives from <see cref="XmlConverter{T}"/> for exactly <paramref name="typeToConvert"/>. The serializer throws an
        /// <see cref="InvalidOperationException"/> if the factory returns <see langword="null"/>.
        /// </returns>
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
