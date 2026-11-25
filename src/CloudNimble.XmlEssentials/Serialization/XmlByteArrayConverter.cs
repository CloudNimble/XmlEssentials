using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts a byte array as <c>xs:base64Binary</c>.
    /// </summary>
    internal sealed class XmlByteArrayConverter : XmlValueConverter<byte[]>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlByteArrayConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override byte[] Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryDecodeBase64(utf8, out var value) ? value : throw XmlException.Create("The value is not valid base64.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, byte[] value, XmlSerializerOptions options)
        {
            writer.WriteBase64StringValue(value);
        }

        #endregion

    }

}
