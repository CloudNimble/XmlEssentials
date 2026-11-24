using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="double"/> as <c>xs:double</c>.
    /// </summary>
    internal sealed class XmlDoubleConverter : XmlValueConverter<double>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlDoubleConverter Instance = new();

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
        internal override void WriteValue(Utf8XmlWriter writer, double value, XmlSerializerOptions options)
        {
            if (options.NumberHandling is XmlNumberHandling.Strict && !double.IsFinite(value))
            {
                throw XmlException.Create("Named floating-point literals are not allowed when NumberHandling is Strict.");
            }

            writer.WriteNumberValue(value);
        }

        #endregion

    }

}
