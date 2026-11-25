using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="ushort"/> as <c>xs:unsignedShort</c>.
    /// </summary>
    internal sealed class XmlUInt16Converter : XmlFormattedValueConverter<ushort>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlUInt16Converter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 5;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override ushort Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseUInt16(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override int Format(ushort value, Span<byte> destination, XmlSerializerOptions options)
        {
            return FormatNumber(value, destination);
        }

        #endregion

    }

}
