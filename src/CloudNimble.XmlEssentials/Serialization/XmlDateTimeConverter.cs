using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="DateTime"/> as <c>xs:dateTime</c>.
    /// </summary>
    internal sealed class XmlDateTimeConverter : XmlFormattedValueConverter<DateTime>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlDateTimeConverter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 40;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override DateTime Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseDateTime(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid dateTime.");
        }

        /// <inheritdoc />
        internal override int Format(DateTime value, Span<byte> destination, XmlSerializerOptions options)
        {
            return XmlLexical.FormatDateTimeWithZone(value, destination);
        }

        #endregion

    }

}
