using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="ulong"/> as <c>xs:unsignedLong</c>.
    /// </summary>
    internal sealed class XmlUInt64Converter : XmlValueConverter<ulong>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlUInt64Converter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override ulong Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseUInt64(utf8, out var value) ? value : throw new XmlException("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, ulong value, XmlSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        #endregion

    }

}
