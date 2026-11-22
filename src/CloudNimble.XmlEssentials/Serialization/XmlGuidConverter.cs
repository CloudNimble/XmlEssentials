using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="Guid"/> in the hyphenated form.
    /// </summary>
    internal sealed class XmlGuidConverter : XmlValueConverter<Guid>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlGuidConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override Guid Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseGuid(utf8, out var value) ? value : throw new XmlException("The value is not a valid GUID.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, Guid value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        #endregion

    }

}
