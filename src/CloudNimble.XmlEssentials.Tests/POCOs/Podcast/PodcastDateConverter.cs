using System;
using System.Globalization;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// Reads and writes a time the way RSS states one: <c>Sat, 04 Oct 2025 15:00:00 -0400</c>.
    /// </summary>
    /// <remarks>
    /// RSS 2.0 takes its dates from RFC 822, with a four-digit year. The offset has no colon, which is not a
    /// form .NET writes, so it is read and written here.
    /// </remarks>
    public sealed class PodcastDateConverter : XmlConverter<DateTimeOffset>
    {

        #region Fields

        private const string DateFormat = "ddd, dd MMM yyyy HH:mm:ss";

        #endregion

        #region Public Methods

        /// <summary>
        /// Formats a time as RSS states one.
        /// </summary>
        /// <param name="value">The time.</param>
        /// <returns>The text, with the offset as a sign and four digits.</returns>
        public static string Format(DateTimeOffset value)
        {
            var offset = value.Offset;
            var sign = offset < TimeSpan.Zero ? '-' : '+';
            return string.Create(CultureInfo.InvariantCulture, $"{value.ToString(DateFormat, CultureInfo.InvariantCulture)} {sign}{Math.Abs(offset.Hours):00}{Math.Abs(offset.Minutes):00}");
        }

        /// <summary>
        /// Parses a time as RSS states one.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The time, with the offset the text states.</returns>
        /// <exception cref="XmlException"><paramref name="text"/> is not a date, a time and an offset of a sign and four digits.</exception>
        public static DateTimeOffset Parse(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            var space = text.LastIndexOf(' ');
            if (space < 0
                || text.Length - space != 6
                || text[space + 1] is not ('+' or '-')
                || !int.TryParse(text.AsSpan(space + 2, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var hours)
                || !int.TryParse(text.AsSpan(space + 4, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var minutes)
                || !DateTime.TryParseExact(text.AsSpan(0, space), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
            {
                throw new XmlException($"'{text}' is not an RSS date.");
            }

            var offset = new TimeSpan(hours, minutes, 0);
            return new DateTimeOffset(local, text[space + 1] is '-' ? -offset : offset);
        }

        /// <inheritdoc />
        public override DateTimeOffset Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            DateTimeOffset value = default;
            if (reader.IsEmptyElement)
            {
                return value;
            }

            while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
            {
                if (reader.TokenType is XmlTokenType.Text or XmlTokenType.CData)
                {
                    value = Parse(reader.GetString()!);
                }
            }

            return value;
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, DateTimeOffset value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(Format(value));
        }

        #endregion

    }

}
