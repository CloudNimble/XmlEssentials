using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="DateTimeOffset"/> as <c>xs:dateTime</c>.
    /// </summary>
    internal sealed class XmlDateTimeOffsetConverter : XmlValueConverter<DateTimeOffset>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlDateTimeOffsetConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override DateTimeOffset Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseDateTimeOffset(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid dateTime.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, DateTimeOffset value, XmlSerializerOptions options)
        {
            if (options.UtcOffsetHandling is XmlUtcOffsetHandling.WriteZ && value.Offset == TimeSpan.Zero)
            {
                // The same instant as a UTC DateTime, which is written with Z.
                writer.WriteStringValue(value.UtcDateTime);
                return;
            }

            writer.WriteStringValue(value);
        }

        #endregion

    }

}
