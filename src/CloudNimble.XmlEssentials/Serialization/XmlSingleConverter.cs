using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="float"/> as <c>xs:float</c>.
    /// </summary>
    internal sealed class XmlSingleConverter : XmlFormattedValueConverter<float>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlSingleConverter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 24;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override float Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            if (!XmlLexical.TryParseSingle(utf8, out var value)
                || (options.NumberHandling is XmlNumberHandling.Strict && !float.IsFinite(value)))
            {
                throw XmlException.Create("The value is not a valid float.");
            }

            return value;
        }

        /// <inheritdoc />
        internal override int Format(float value, Span<byte> destination, XmlSerializerOptions options)
        {
            return FormatFloat(value, destination, options);
        }

        #endregion

    }

}
