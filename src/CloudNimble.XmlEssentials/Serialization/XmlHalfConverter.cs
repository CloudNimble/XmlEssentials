using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="Half"/> in the lexical form of <c>xs:float</c>.
    /// </summary>
    internal sealed class XmlHalfConverter : XmlFormattedValueConverter<Half>
    {

        #region Fields

        internal static readonly XmlHalfConverter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 24;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override Half Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            if (!XmlLexical.TryParseHalf(utf8, out var value)
                || (options.NumberHandling is XmlNumberHandling.Strict && !Half.IsFinite(value)))
            {
                throw XmlException.Create("The value is not a valid float.");
            }

            return value;
        }

        /// <inheritdoc />
        internal override int Format(Half value, Span<byte> destination, XmlSerializerOptions options)
        {
            return FormatFloat(value, destination, options);
        }

        #endregion

    }

}
