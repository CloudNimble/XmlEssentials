using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="Half"/> in the lexical form of <c>xs:float</c>.
    /// </summary>
    internal sealed class XmlHalfConverter : XmlValueConverter<Half>
    {

        #region Fields

        internal static readonly XmlHalfConverter Instance = new();

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
        internal override void WriteValue(Utf8XmlWriter writer, Half value, XmlSerializerOptions options)
        {
            if (options.NumberHandling is XmlNumberHandling.Strict && !Half.IsFinite(value))
            {
                throw XmlException.Create("Named floating-point literals are not allowed when NumberHandling is Strict.");
            }

            if (Half.IsNaN(value))
            {
                writer.WriteStringValue("NaN"u8);
            }
            else if (Half.IsPositiveInfinity(value))
            {
                writer.WriteStringValue("INF"u8);
            }
            else if (Half.IsNegativeInfinity(value))
            {
                writer.WriteStringValue("-INF"u8);
            }
            else
            {
                writer.WriteFormatted(value, default);
            }
        }

        #endregion

    }

}
