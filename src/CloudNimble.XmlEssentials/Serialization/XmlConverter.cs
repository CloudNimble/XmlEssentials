using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts a CLR type to and from XML.
    /// </summary>
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
        /// Returns whether this converter handles <paramref name="typeToConvert"/>.
        /// </summary>
        /// <param name="typeToConvert">The CLR type.</param>
        /// <returns><see langword="true"/> if this converter can convert the type.</returns>
        public abstract bool CanConvert(Type typeToConvert);

        #endregion

        #region Private Methods

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
