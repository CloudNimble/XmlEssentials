using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="UInt128"/>, a 128-bit unsigned integer, as decimal digits.
    /// </summary>
    internal sealed class XmlUInt128Converter : XmlValueConverter<UInt128>
    {

        #region Fields

        internal static readonly XmlUInt128Converter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override UInt128 Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseUInt128(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, UInt128 value, XmlSerializerOptions options)
        {
            writer.WriteFormatted(value, default);
        }

        #endregion

    }

}
