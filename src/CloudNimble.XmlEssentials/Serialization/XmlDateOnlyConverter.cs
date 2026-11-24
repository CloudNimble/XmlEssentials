using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="DateOnly"/> as <c>xs:date</c>.
    /// </summary>
    internal sealed class XmlDateOnlyConverter : XmlValueConverter<DateOnly>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlDateOnlyConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override DateOnly Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseDateOnly(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid date.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, DateOnly value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        #endregion

    }

}
