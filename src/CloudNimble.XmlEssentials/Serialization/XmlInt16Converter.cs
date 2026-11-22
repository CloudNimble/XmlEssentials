using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="short"/> as <c>xs:short</c>.
    /// </summary>
    internal sealed class XmlInt16Converter : XmlValueConverter<short>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlInt16Converter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override short Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseInt16(utf8, out var value) ? value : throw new XmlException("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, short value, XmlSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        #endregion

    }

}
