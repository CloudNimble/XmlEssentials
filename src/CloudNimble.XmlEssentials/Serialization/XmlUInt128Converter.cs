using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="UInt128"/>, a 128-bit unsigned integer, as decimal digits.
    /// </summary>
    internal sealed class XmlUInt128Converter : XmlFormattedValueConverter<UInt128>
    {

        #region Fields

        internal static readonly XmlUInt128Converter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 39;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override UInt128 Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseUInt128(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override int Format(UInt128 value, Span<byte> destination, XmlSerializerOptions options)
        {
            return FormatNumber(value, destination);
        }

        #endregion

    }

}
