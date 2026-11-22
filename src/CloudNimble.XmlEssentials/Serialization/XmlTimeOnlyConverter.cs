using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="TimeOnly"/> as <c>xs:time</c>.
    /// </summary>
    internal sealed class XmlTimeOnlyConverter : XmlValueConverter<TimeOnly>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlTimeOnlyConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override TimeOnly Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseTimeOnly(utf8, out var value) ? value : throw new XmlException("The value is not a valid time.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, TimeOnly value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        #endregion

    }

}
