using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="ushort"/> as <c>xs:unsignedShort</c>.
    /// </summary>
    internal sealed class XmlUInt16Converter : XmlValueConverter<ushort>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlUInt16Converter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override ushort Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseUInt16(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, ushort value, XmlSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        #endregion

    }

}
