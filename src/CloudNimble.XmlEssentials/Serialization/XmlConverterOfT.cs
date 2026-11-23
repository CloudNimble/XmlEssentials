using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <typeparamref name="T"/> to and from XML.
    /// </summary>
    /// <typeparam name="T">The CLR type.</typeparam>
    /// <remarks>
    /// <see cref="Read"/> is invoked with the reader on the start tag of that value.
    /// The converter consumes through the matching end tag (or handles an empty element).
    /// </remarks>
    public abstract class XmlConverter<T> : XmlConverter
    {

        #region Public Properties

        /// <summary>
        /// Gets whether <see cref="Read"/> is invoked for an element marked <c>xsi:nil="true"</c>.
        /// When <see langword="false"/>, the serializer returns <see langword="default"/> without calling <see cref="Read"/>.
        /// </summary>
        public virtual bool HandleNull => false;

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(T);
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="reader">The reader, on the start tag.</param>
        /// <param name="typeToConvert">The CLR type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The value.</returns>
        public abstract T? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options);

        /// <summary>
        /// Reads a value from the current attribute.
        /// </summary>
        /// <param name="reader">The reader, on an attribute.</param>
        /// <param name="typeToConvert">The CLR type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The value.</returns>
        public virtual T? ReadAsAttribute(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            throw new InvalidOperationException($"Converter '{GetType()}' does not support attributes.");
        }

        /// <summary>
        /// Reads a value from already-decoded UTF-8 lexical form (simple content).
        /// </summary>
        /// <param name="utf8">The UTF-8 lexical form.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The value.</returns>
        public virtual T? ReadLexical(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            throw new InvalidOperationException($"Converter '{GetType()}' does not support simple content.");
        }

        /// <summary>
        /// Writes a value of type <typeparamref name="T"/> as element content.
        /// </summary>
        /// <param name="writer">The writer, inside a start tag.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The serializer options.</param>
        public abstract void Write(Utf8XmlWriter writer, T value, XmlSerializerOptions options);

        /// <summary>
        /// Writes a value as an attribute named <paramref name="name"/>.
        /// </summary>
        /// <param name="writer">The writer, on an open start tag.</param>
        /// <param name="name">The attribute name.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The serializer options.</param>
        public virtual void WriteAsAttribute(Utf8XmlWriter writer, XmlName name, T value, XmlSerializerOptions options)
        {
            throw new InvalidOperationException($"Converter '{GetType()}' does not support attributes.");
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal sealed override XmlTypeInfo? CreateTypeInfo(Type typeToConvert, XmlSerializerOptions options)
        {
            return typeToConvert == typeof(T) ? CreateTypeInfo(options) : null;
        }

        /// <summary>
        /// Creates type info for <typeparamref name="T"/>.
        /// </summary>
        /// <param name="options">The serializer options.</param>
        /// <returns>The type info.</returns>
        private protected virtual XmlTypeInfo CreateTypeInfo(XmlSerializerOptions options)
        {
            return new XmlConverterTypeInfo<T>(this, options);
        }

        /// <inheritdoc />
        internal sealed override XmlConverter Resolve(Type typeToConvert, XmlSerializerOptions options)
        {
            return this;
        }

        #endregion

    }

}
