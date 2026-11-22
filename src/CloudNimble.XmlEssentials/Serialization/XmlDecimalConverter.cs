using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="decimal"/> as <c>xs:decimal</c>.
    /// </summary>
    internal sealed class XmlDecimalConverter : XmlValueConverter<decimal>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlDecimalConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override decimal Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseDecimal(utf8, out var value) ? value : throw new XmlException("The value is not a valid decimal.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, decimal value, XmlSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        #endregion

    }

}
