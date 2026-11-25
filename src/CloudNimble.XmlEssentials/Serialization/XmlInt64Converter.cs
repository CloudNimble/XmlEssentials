using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="long"/> as <c>xs:long</c>.
    /// </summary>
    internal sealed class XmlInt64Converter : XmlFormattedValueConverter<long>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlInt64Converter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 20;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override long Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseInt64(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override int Format(long value, Span<byte> destination, XmlSerializerOptions options)
        {
            return FormatNumber(value, destination);
        }

        #endregion

    }

}
