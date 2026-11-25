using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="double"/> as <c>xs:double</c>.
    /// </summary>
    internal sealed class XmlDoubleConverter : XmlFormattedValueConverter<double>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlDoubleConverter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 32;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override double Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            if (!XmlLexical.TryParseDouble(utf8, out var value)
                || (options.NumberHandling is XmlNumberHandling.Strict && !double.IsFinite(value)))
            {
                throw XmlException.Create("The value is not a valid double.");
            }

            return value;
        }

        /// <inheritdoc />
        internal override int Format(double value, Span<byte> destination, XmlSerializerOptions options)
        {
            return FormatFloat(value, destination, options);
        }

        #endregion

    }

}
