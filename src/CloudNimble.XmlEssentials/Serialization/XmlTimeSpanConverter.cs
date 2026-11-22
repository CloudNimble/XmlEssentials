using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="TimeSpan"/> as <c>xs:duration</c>.
    /// </summary>
    internal sealed class XmlTimeSpanConverter : XmlValueConverter<TimeSpan>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlTimeSpanConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override TimeSpan Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseDuration(utf8, out var value) ? value : throw new XmlException("The value is not a valid duration.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, TimeSpan value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        #endregion

    }

}
