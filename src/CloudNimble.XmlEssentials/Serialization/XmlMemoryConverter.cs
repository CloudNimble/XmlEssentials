using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="Memory{T}"/> of bytes as <c>xs:base64Binary</c>.
    /// </summary>
    internal sealed class XmlMemoryConverter : XmlValueConverter<Memory<byte>>
    {

        #region Fields

        internal static readonly XmlMemoryConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override Memory<byte> Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryDecodeBase64(utf8, out var value) ? value : throw XmlException.Create("The value is not valid base64.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, Memory<byte> value, XmlSerializerOptions options)
        {
            writer.WriteBase64StringValue(value.Span);
        }

        #endregion

    }

}
