using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="Int128"/>, a 128-bit signed integer, as decimal digits.
    /// </summary>
    internal sealed class XmlInt128Converter : XmlValueConverter<Int128>
    {

        #region Fields

        internal static readonly XmlInt128Converter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override Int128 Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseInt128(utf8, out var value) ? value : throw new XmlException("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, Int128 value, XmlSerializerOptions options)
        {
            writer.WriteFormatted(value, default);
        }

        #endregion

    }

}
