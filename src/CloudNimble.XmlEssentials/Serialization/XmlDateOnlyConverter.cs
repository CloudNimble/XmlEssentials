using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="DateOnly"/> as <c>xs:date</c>.
    /// </summary>
    internal sealed class XmlDateOnlyConverter : XmlFormattedValueConverter<DateOnly>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlDateOnlyConverter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 10;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override DateOnly Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseDateOnly(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid date.");
        }

        /// <inheritdoc />
        internal override int Format(DateOnly value, Span<byte> destination, XmlSerializerOptions options)
        {
            return XmlLexical.FormatDate(value.Year, value.Month, value.Day, destination);
        }

        #endregion

    }

}
