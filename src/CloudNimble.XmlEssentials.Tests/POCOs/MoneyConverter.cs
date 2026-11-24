using System;
using System.Globalization;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Converts <see cref="Money"/> as one text value, so it can be an element, an attribute, or the text of an element.
    /// </summary>
    public sealed class MoneyConverter : XmlConverter<Money>
    {

        #region Public Methods

        /// <inheritdoc />
        public override Money Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            var value = default(Money);
            while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
            {
                value = ReadLexical(reader.ValueSpan, options);
            }

            return value;
        }

        /// <inheritdoc />
        public override Money ReadAsAttribute(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            return ReadLexical(reader.ValueSpan, options);
        }

        /// <inheritdoc />
        public override Money ReadLexical(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            var parts = Encoding.UTF8.GetString(utf8).Split(' ');
            return parts.Length == 2 && decimal.TryParse(parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
                ? new Money(amount, parts[1])
                : throw new XmlException("The value is not an amount and a currency.");
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, Money value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value.Amount.ToString(CultureInfo.InvariantCulture) + " " + value.Currency);
        }

        /// <inheritdoc />
        public override void WriteAsAttribute(Utf8XmlWriter writer, XmlName name, Money value, XmlSerializerOptions options)
        {
            writer.WriteStartAttribute(name.Prefix.Length > 0 ? name.Prefix : null, name.LocalName, name.NamespaceUri);
            Write(writer, value, options);
            writer.WriteEndAttribute();
        }

        #endregion

    }

}
