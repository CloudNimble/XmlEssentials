using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts an object or value to or from XML.
    /// </summary>
    /// <typeparam name="T">
    /// The type of object or value handled by the converter.
    /// </typeparam>
    /// <remarks>
    /// <see cref="Read"/> is called with the reader positioned on the start tag of the value.
    /// The converter consumes through the matching end tag, or handles an empty element.
    /// </remarks>
    public abstract class XmlConverter<T> : XmlConverter
    {

        #region Public Properties

        /// <summary>
        /// Gets a value that indicates whether an element marked <c>xsi:nil="true"</c> is passed to the converter on deserialization.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if <see cref="Read"/> is called for an element marked <c>xsi:nil="true"</c>; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// When the value is <see langword="false"/>, the serializer returns <see langword="null"/> for a nil element without calling <see cref="Read"/>,
        /// and throws an <see cref="XmlException"/> when <typeparamref name="T"/> is a value type that cannot be <see langword="null"/>.
        /// </remarks>
        public virtual bool HandleNull => false;

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(T);
        }

        /// <summary>
        /// When overridden in a derived class, reads and converts the XML element to type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="reader">The reader, positioned on the start tag of the element.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The serialization options to use.</param>
        /// <returns>
        /// The converted value.
        /// </returns>
        public abstract T? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options);

        /// <summary>
        /// Reads and converts the value of the current XML attribute to type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="reader">The reader, positioned on an attribute.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The serialization options to use.</param>
        /// <returns>
        /// The converted value.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The converter does not support reading from attributes.
        /// </exception>
        /// <remarks>
        /// The default implementation throws an <see cref="InvalidOperationException"/>. Override this method in a converter
        /// whose type can be mapped to an XML attribute.
        /// </remarks>
        public virtual T? ReadAsAttribute(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            throw new InvalidOperationException($"Converter '{GetType()}' does not support attributes.");
        }

        /// <summary>
        /// Converts the lexical form of a value, as decoded UTF-8 text content, to type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="utf8">The lexical form of the value, as decoded UTF-8 text.</param>
        /// <param name="options">The serialization options to use.</param>
        /// <returns>
        /// The converted value.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The converter does not support reading from text content.
        /// </exception>
        /// <remarks>
        /// The default implementation throws an <see cref="InvalidOperationException"/>. Override this method in a converter
        /// whose type can be read from simple content.
        /// </remarks>
        public virtual T? ReadLexical(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            throw new InvalidOperationException($"Converter '{GetType()}' does not support simple content.");
        }

        /// <summary>
        /// When overridden in a derived class, writes the specified value as the content of the current XML element.
        /// </summary>
        /// <param name="writer">The writer to write to, positioned inside the start tag of the element.</param>
        /// <param name="value">The value to convert to XML.</param>
        /// <param name="options">The serialization options to use.</param>
        public abstract void Write(Utf8XmlWriter writer, T value, XmlSerializerOptions options);

        /// <summary>
        /// Writes the specified value as an XML attribute with the specified name.
        /// </summary>
        /// <param name="writer">The writer to write to, positioned on an open start tag.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value to convert to XML.</param>
        /// <param name="options">The serialization options to use.</param>
        /// <exception cref="InvalidOperationException">
        /// The converter does not support writing attributes.
        /// </exception>
        /// <remarks>
        /// The default implementation throws an <see cref="InvalidOperationException"/>. Override this method in a converter
        /// whose type can be mapped to an XML attribute.
        /// </remarks>
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
