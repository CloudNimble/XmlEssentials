using System;
using System.Globalization;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// Converts <see cref="DateOnly"/> to and from eight digits such as <c>20261123</c>, in an element, an
    /// attribute, or the text of an element.
    /// </summary>
    public sealed class CompactDateConverter : XmlConverter<DateOnly>
    {

        #region Fields

        private const string Format = "yyyyMMdd";

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override DateOnly Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            DateOnly value = default;
            if (reader.IsEmptyElement)
            {
                return value;
            }

            while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
            {
                if (reader.TokenType is XmlTokenType.Text)
                {
                    value = ReadLexical(reader.ValueSpan, options);
                }
            }

            return value;
        }

        /// <inheritdoc />
        public override DateOnly ReadAsAttribute(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            return ReadLexical(reader.ValueSpan, options);
        }

        /// <inheritdoc />
        public override DateOnly ReadLexical(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            string text = Encoding.UTF8.GetString(utf8);
            return DateOnly.TryParseExact(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly value)
                ? value
                : throw new XmlException($"'{text}' is not a date in the form {Format}.");
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, DateOnly value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
        }

        /// <inheritdoc />
        public override void WriteAsAttribute(Utf8XmlWriter writer, XmlName name, DateOnly value, XmlSerializerOptions options)
        {
            writer.WriteAttributeString(name.LocalName, value.ToString(Format, CultureInfo.InvariantCulture));
        }

        #endregion

    }

}
