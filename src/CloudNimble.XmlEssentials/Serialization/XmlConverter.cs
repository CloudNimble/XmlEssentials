using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts an object or value to or from XML.
    /// </summary>
    /// <remarks>
    /// Derive from <see cref="XmlConverter{T}"/> or <see cref="XmlConverterFactory"/>. This class cannot be derived from directly.
    /// </remarks>
    public abstract class XmlConverter
    {

        #region Constructors

        /// <summary>
        /// Limits derivation to <see cref="XmlConverter{T}"/> and <see cref="XmlConverterFactory"/>.
        /// </summary>
        private protected XmlConverter()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// When overridden in a derived class, determines whether the converter instance can convert the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type to check.</param>
        /// <returns>
        /// <see langword="true"/> if the instance can convert <paramref name="typeToConvert"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public abstract bool CanConvert(Type typeToConvert);

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets whether this is one of the built-in converters that read and write a value from its lexical form,
        /// which the serializer can call directly.
        /// </summary>
        internal virtual bool IsValueConverter => false;

        /// <summary>
        /// Creates type info that reads and writes <paramref name="typeToConvert"/> with this converter.
        /// </summary>
        /// <param name="typeToConvert">The CLR type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The type info, or <see langword="null"/> when this converter cannot produce values of that exact type.</returns>
        internal abstract XmlTypeInfo? CreateTypeInfo(Type typeToConvert, XmlSerializerOptions options);

        /// <summary>
        /// Returns the converter that does the work for <paramref name="typeToConvert"/>: this converter, or the
        /// one a factory creates.
        /// </summary>
        /// <param name="typeToConvert">The CLR type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The converter.</returns>
        internal abstract XmlConverter Resolve(Type typeToConvert, XmlSerializerOptions options);

        #endregion

    }

}
