using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="ulong"/> as <c>xs:unsignedLong</c>.
    /// </summary>
    internal sealed class XmlUInt64Converter : XmlFormattedValueConverter<ulong>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlUInt64Converter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 20;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override ulong Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseUInt64(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override int Format(ulong value, Span<byte> destination, XmlSerializerOptions options)
        {
            return FormatNumber(value, destination);
        }

        #endregion

    }

}
