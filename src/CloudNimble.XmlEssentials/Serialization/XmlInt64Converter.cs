using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="long"/> as <c>xs:long</c>.
    /// </summary>
    internal sealed class XmlInt64Converter : XmlValueConverter<long>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlInt64Converter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override long Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseInt64(utf8, out var value) ? value : throw new XmlException("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, long value, XmlSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        #endregion

    }

}
