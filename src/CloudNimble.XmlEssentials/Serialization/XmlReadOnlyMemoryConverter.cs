using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="ReadOnlyMemory{T}"/> of bytes as <c>xs:base64Binary</c>.
    /// </summary>
    internal sealed class XmlReadOnlyMemoryConverter : XmlValueConverter<ReadOnlyMemory<byte>>
    {

        #region Fields

        internal static readonly XmlReadOnlyMemoryConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override ReadOnlyMemory<byte> Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryDecodeBase64(utf8, out var value) ? value : throw new XmlException("The value is not valid base64.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, ReadOnlyMemory<byte> value, XmlSerializerOptions options)
        {
            writer.WriteBase64StringValue(value.Span);
        }

        #endregion

    }

}
