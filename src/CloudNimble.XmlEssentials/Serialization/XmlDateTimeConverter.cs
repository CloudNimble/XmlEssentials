using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="DateTime"/> as <c>xs:dateTime</c>.
    /// </summary>
    internal sealed class XmlDateTimeConverter : XmlValueConverter<DateTime>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlDateTimeConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override DateTime Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseDateTime(utf8, out var value) ? value : throw new XmlException("The value is not a valid dateTime.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, DateTime value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        #endregion

    }

}
