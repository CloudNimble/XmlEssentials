using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="float"/> as <c>xs:float</c>.
    /// </summary>
    internal sealed class XmlSingleConverter : XmlValueConverter<float>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlSingleConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override float Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            if (!XmlLexical.TryParseSingle(utf8, out var value)
                || (options.NumberHandling is XmlNumberHandling.Strict && !float.IsFinite(value)))
            {
                throw new XmlException("The value is not a valid float.");
            }

            return value;
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, float value, XmlSerializerOptions options)
        {
            if (options.NumberHandling is XmlNumberHandling.Strict && !float.IsFinite(value))
            {
                throw new XmlException("Named floating-point literals are not allowed when NumberHandling is Strict.");
            }

            writer.WriteNumberValue(value);
        }

        #endregion

    }

}
