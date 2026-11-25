using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="TimeOnly"/> as <c>xs:time</c>.
    /// </summary>
    internal sealed class XmlTimeOnlyConverter : XmlFormattedValueConverter<TimeOnly>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlTimeOnlyConverter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 16;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override TimeOnly Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseTimeOnly(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid time.");
        }

        /// <inheritdoc />
        internal override int Format(TimeOnly value, Span<byte> destination, XmlSerializerOptions options)
        {
            return XmlLexical.FormatTime(value.Ticks, destination);
        }

        #endregion

    }

}
