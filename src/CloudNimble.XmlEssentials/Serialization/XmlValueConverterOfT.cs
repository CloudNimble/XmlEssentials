using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Base for converters whose value is the character content of one element or one attribute.
    /// </summary>
    /// <typeparam name="T">The CLR type.</typeparam>
    internal abstract class XmlValueConverter<T> : XmlConverter<T>
    {

        #region Public Methods

        /// <inheritdoc />
        public sealed override T Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            return TryRead(ref reader, options, out var value)
                ? value
                : throw XmlException.Create("The element is incomplete: more data is required.");
        }

        /// <inheritdoc />
        public sealed override T ReadAsAttribute(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            return Parse(reader.ValueSpan, options);
        }

        /// <inheritdoc />
        public sealed override T ReadLexical(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return Parse(utf8, options);
        }

        /// <inheritdoc />
        public sealed override void Write(Utf8XmlWriter writer, T value, XmlSerializerOptions options)
        {
            WriteValue(writer, value, options);
        }

        /// <inheritdoc />
        public sealed override void WriteAsAttribute(Utf8XmlWriter writer, XmlName name, T value, XmlSerializerOptions options)
        {
            XmlConverterHelpers.WriteStartAttribute(writer, name);
            WriteValue(writer, value, options);
            writer.WriteEndAttribute();
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal sealed override bool IsValueConverter => true;

        /// <summary>
        /// Parses the lexical form.
        /// </summary>
        /// <param name="utf8">The UTF-8 lexical form, with character and entity references already expanded.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The value.</returns>
        /// <exception cref="XmlException">The lexical form is not valid for <typeparamref name="T"/>.</exception>
        internal abstract T Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options);

        /// <summary>
        /// Reads the element <paramref name="reader"/> is on, unless the reader's block ends before the
        /// element does.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="value">When this method returns <see langword="true"/>, the value.</param>
        /// <returns>
        /// <see langword="false"/> when more data is required; the reader is then part way through the
        /// element and the caller restores it. This is what lets a stream of values be read in one pass.
        /// </returns>
        internal bool TryRead(ref Utf8XmlReader reader, XmlSerializerOptions options, [MaybeNullWhen(false)] out T value)
        {
            byte[]? rented = null;
            try
            {
                if (!XmlConverterHelpers.TryReadContent(ref reader, ref rented, out var content))
                {
                    value = default;
                    return false;
                }

                value = Parse(content, options);
                return true;
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        /// <summary>
        /// Writes <paramref name="value"/> as an attribute in no namespace whose name was built ahead of time.
        /// </summary>
        /// <param name="writer">The writer, which has a start tag open.</param>
        /// <param name="local">The attribute name, an NCName that was checked when <paramref name="head"/> was built.</param>
        /// <param name="head">Everything up to the value: a space, the name, <c>="</c>.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The serializer options.</param>
        internal virtual void WriteAttribute(Utf8XmlWriter writer, ReadOnlySpan<byte> local, ReadOnlySpan<byte> head, T value, XmlSerializerOptions options)
        {
            writer.WriteStartAttributeTrusted(local, head);
            WriteValue(writer, value, options);
            writer.WriteEndAttribute();
        }

        /// <summary>
        /// Writes <paramref name="value"/> as the only content of an element whose tags were built ahead of
        /// time, with no element frame.
        /// </summary>
        /// <param name="writer">The writer, which is inside an element.</param>
        /// <param name="openTag">The whole start tag, <c>&lt;name&gt;</c>.</param>
        /// <param name="closeTag">The whole end tag, <c>&lt;/name&gt;</c>.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The serializer options.</param>
        internal virtual void WriteLeaf(Utf8XmlWriter writer, ReadOnlySpan<byte> openTag, ReadOnlySpan<byte> closeTag, T value, XmlSerializerOptions options)
        {
            writer.WriteLeafStart(openTag);
            WriteValue(writer, value, options);
            writer.WriteLeafEnd(closeTag);
        }

        /// <summary>
        /// Writes <paramref name="value"/> as element or attribute content.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The serializer options.</param>
        internal abstract void WriteValue(Utf8XmlWriter writer, T value, XmlSerializerOptions options);

        #endregion

    }

}
