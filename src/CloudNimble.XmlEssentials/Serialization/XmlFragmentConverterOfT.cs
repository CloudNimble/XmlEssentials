using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts a value that is XML already: an element of a parsed document, a document, or a node of a mutable tree.
    /// </summary>
    /// <typeparam name="T">The type that holds the XML.</typeparam>
    /// <remarks>
    /// As the document element the value is the document: it is written under its own name, and any
    /// document element is read. As a member the value is the one element inside the member's element, so
    /// the member keeps its name and the value keeps its own.
    /// </remarks>
    internal abstract class XmlFragmentConverter<T> : XmlConverter<T>
    {

        #region Public Methods

        /// <inheritdoc />
        public sealed override T? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            var value = default(T);
            if (reader.IsEmptyElement)
            {
                return value;
            }

            var found = false;
            while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
            {
                if (reader.TokenType is not XmlTokenType.StartElement)
                {
                    continue;
                }

                if (found)
                {
                    throw XmlException.Create("The element holds more than one element where one XML value is expected.");
                }

                value = ReadValue(ref reader);
                found = true;
            }

            return value;
        }

        /// <inheritdoc />
        public sealed override void Write(Utf8XmlWriter writer, T value, XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            if (!IsEmpty(value))
            {
                WriteValue(writer, value);
            }
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        private protected sealed override XmlTypeInfo CreateTypeInfo(XmlSerializerOptions options)
        {
            return new XmlFragmentTypeInfo<T>(this, options);
        }

        /// <summary>
        /// Returns whether <paramref name="value"/> holds no XML, so that a member is written as an empty element.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><see langword="true"/> if there is nothing to write.</returns>
        internal virtual bool IsEmpty(T value)
        {
            return false;
        }

        /// <summary>
        /// Reads the element the reader is on as the value.
        /// </summary>
        /// <param name="reader">The reader, on the start tag. It is left on the end tag, or on the start tag of an empty element.</param>
        /// <returns>The value.</returns>
        internal abstract T ReadValue(ref Utf8XmlReader reader);

        /// <summary>
        /// Writes the value as the element it is.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        internal abstract void WriteValue(Utf8XmlWriter writer, T value);

        #endregion

    }

}
