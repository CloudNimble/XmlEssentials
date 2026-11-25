using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="TimeSpan"/> as <c>xs:duration</c>.
    /// </summary>
    internal sealed class XmlTimeSpanConverter : XmlFormattedValueConverter<TimeSpan>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlTimeSpanConverter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 32;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override TimeSpan Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseDuration(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid duration.");
        }

        /// <inheritdoc />
        internal override int Format(TimeSpan value, Span<byte> destination, XmlSerializerOptions options)
        {
            return XmlLexical.FormatDuration(value, destination);
        }

        #endregion

    }

}
