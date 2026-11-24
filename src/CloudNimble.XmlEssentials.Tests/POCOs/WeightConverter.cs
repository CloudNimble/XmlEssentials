using System;
using System.Buffers.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Converts <see cref="Weight"/> as a number followed by <c>g</c>.
    /// </summary>
    public sealed class WeightConverter : XmlConverter<Weight>
    {

        #region Public Methods

        /// <inheritdoc />
        public override Weight Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            var value = default(Weight);
            while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
            {
                value = ReadLexical(reader.ValueSpan, options);
            }

            return value;
        }

        /// <inheritdoc />
        public override Weight ReadAsAttribute(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            return ReadLexical(reader.ValueSpan, options);
        }

        /// <inheritdoc />
        public override Weight ReadLexical(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return utf8.EndsWith("g"u8) && Utf8Parser.TryParse(utf8[..^1], out int grams, out var consumed) && consumed == utf8.Length - 1
                ? new Weight(grams)
                : throw new XmlException("The value is not a weight in grams.");
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, Weight value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value.Grams + "g");
        }

        /// <inheritdoc />
        public override void WriteAsAttribute(Utf8XmlWriter writer, XmlName name, Weight value, XmlSerializerOptions options)
        {
            writer.WriteStartAttribute(name.LocalName);
            Write(writer, value, options);
            writer.WriteEndAttribute();
        }

        #endregion

    }

}
