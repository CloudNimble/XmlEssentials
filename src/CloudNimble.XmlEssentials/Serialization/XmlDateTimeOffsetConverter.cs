using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="DateTimeOffset"/> as <c>xs:dateTime</c>.
    /// </summary>
    internal sealed class XmlDateTimeOffsetConverter : XmlFormattedValueConverter<DateTimeOffset>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlDateTimeOffsetConverter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 40;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override DateTimeOffset Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseDateTimeOffset(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid dateTime.");
        }

        /// <inheritdoc />
        internal override int Format(DateTimeOffset value, Span<byte> destination, XmlSerializerOptions options)
        {
            if (options.UtcOffsetHandling is XmlUtcOffsetHandling.WriteZ && value.Offset == TimeSpan.Zero)
            {
                // The same instant as a UTC DateTime, which is written with Z.
                return XmlLexical.FormatDateTimeWithZone(value.UtcDateTime, destination);
            }

            var written = XmlLexical.FormatDateTime(value.DateTime, destination);
            return written + XmlLexical.FormatOffset(value.Offset, destination[written..]);
        }

        #endregion

    }

}
