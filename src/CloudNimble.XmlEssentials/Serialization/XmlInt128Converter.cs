using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="Int128"/>, a 128-bit signed integer, as decimal digits.
    /// </summary>
    internal sealed class XmlInt128Converter : XmlFormattedValueConverter<Int128>
    {

        #region Fields

        internal static readonly XmlInt128Converter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 40;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override Int128 Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseInt128(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override int Format(Int128 value, Span<byte> destination, XmlSerializerOptions options)
        {
            return FormatNumber(value, destination);
        }

        #endregion

    }

}
