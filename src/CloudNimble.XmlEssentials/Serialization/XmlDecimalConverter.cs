using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="decimal"/> as <c>xs:decimal</c>.
    /// </summary>
    internal sealed class XmlDecimalConverter : XmlFormattedValueConverter<decimal>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlDecimalConverter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 32;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override decimal Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseDecimal(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid decimal.");
        }

        /// <inheritdoc />
        internal override int Format(decimal value, Span<byte> destination, XmlSerializerOptions options)
        {
            return FormatNumber(value, destination);
        }

        #endregion

    }

}
