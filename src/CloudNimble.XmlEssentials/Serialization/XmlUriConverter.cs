using System;
using System.Text;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="Uri"/> as <c>xs:anyURI</c>, relative or absolute.
    /// </summary>
    internal sealed class XmlUriConverter : XmlValueConverter<Uri>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlUriConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override Uri Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return Uri.TryCreate(Encoding.UTF8.GetString(XmlLexical.Collapse(utf8)), UriKind.RelativeOrAbsolute, out var value)
                ? value
                : throw new XmlException("The value is not a valid URI.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, Uri value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value.OriginalString);
        }

        #endregion

    }

}
