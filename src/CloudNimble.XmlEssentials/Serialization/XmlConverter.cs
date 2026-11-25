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
        /// Gets whether the converter can read an element into a value that already exists.
        /// </summary>
        /// <value>
        /// <see langword="true"/> for the converters of objects and dictionaries that the library creates; otherwise,
        /// <see langword="false"/>. A converter the caller wrote creates its value, so a member it reads is replaced.
        /// </value>
        internal virtual bool CanPopulate => false;

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
        /// Reads the element <paramref name="reader"/> is on into <paramref name="existing"/>.
        /// </summary>
        /// <param name="reader">The reader, on a start tag that is not marked nil.</param>
        /// <param name="existing">The value to populate. A value type is boxed, and the box is what is changed.</param>
        /// <param name="options">The serializer options.</param>
        /// <exception cref="NotSupportedException">The converter cannot populate: <see cref="CanPopulate"/> is <see langword="false"/>.</exception>
        internal virtual void Populate(ref Utf8XmlReader reader, object existing, XmlSerializerOptions options)
        {
            throw new NotSupportedException($"The converter '{GetType()}' cannot populate an existing value.");
        }

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
