using System;
using System.Buffers.Text;
using System.Globalization;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// An <see cref="int"/> converter that reads and writes lower-case hexadecimal, as an element, an attribute,
    /// or simple content, so its output cannot be mistaken for the built-in converter's.
    /// </summary>
    public sealed class HexInt32Converter : XmlConverter<int>
    {

        #region Public Methods

        /// <inheritdoc />
        public override int Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            var value = 0;
            while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
            {
                value = ReadLexical(reader.ValueSpan, options);
            }

            return value;
        }

        /// <inheritdoc />
        public override int ReadAsAttribute(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            return ReadLexical(reader.ValueSpan, options);
        }

        /// <inheritdoc />
        public override int ReadLexical(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return Utf8Parser.TryParse(utf8, out int value, out var consumed, 'x') && consumed == utf8.Length
                ? value
                : throw new XmlException("The value is not hexadecimal.");
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, int value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("x", CultureInfo.InvariantCulture));
        }

        /// <inheritdoc />
        public override void WriteAsAttribute(Utf8XmlWriter writer, XmlName name, int value, XmlSerializerOptions options)
        {
            writer.WriteStartAttribute(name.Prefix.Length > 0 ? name.Prefix : null, name.LocalName, name.NamespaceUri);
            Write(writer, value, options);
            writer.WriteEndAttribute();
        }

        #endregion

    }

}
