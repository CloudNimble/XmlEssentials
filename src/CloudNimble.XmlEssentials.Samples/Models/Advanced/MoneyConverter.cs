using System;
using System.Globalization;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// Converts <see cref="Money"/> to and from element text such as <c>129.90 USD</c>.
    /// </summary>
    public sealed class MoneyConverter : XmlConverter<Money>
    {

        #region Public Methods

        /// <inheritdoc />
        public override Money Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            Money value = default;
            if (reader.IsEmptyElement)
            {
                return value;
            }

            while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
            {
                if (reader.TokenType is XmlTokenType.Text or XmlTokenType.CData)
                {
                    value = Parse(reader.GetString()!);
                }
            }

            return value;
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, Money value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(string.Create(CultureInfo.InvariantCulture, $"{value.Amount:0.00} {value.Currency}"));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Parses an amount, a space, and a currency code.
        /// </summary>
        /// <param name="text">The text of the element.</param>
        /// <returns>The value the text describes.</returns>
        /// <exception cref="XmlException"><paramref name="text"/> is not an amount followed by a currency code.</exception>
        private static Money Parse(string text)
        {
            int space = text.IndexOf(' ');
            if (space < 0 || !decimal.TryParse(text.AsSpan(0, space), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount))
            {
                throw new XmlException($"'{text}' is not an amount followed by a currency code.");
            }

            return new Money(amount, text[(space + 1)..]);
        }

        #endregion

    }

}
