using System;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Podcast
{

    /// <summary>
    /// Writes a string as a CDATA section, as a podcast host writes titles and HTML, and reads it from a
    /// CDATA section or from text.
    /// </summary>
    /// <remarks>
    /// <see cref="XmlCDataAttribute"/> makes a member the content of its containing element, so it cannot be
    /// one child element among others. This converter is for that case: a member with an element of its own.
    /// </remarks>
    public sealed class PodcastCDataConverter : XmlConverter<string>
    {

        #region Public Methods

        /// <inheritdoc />
        public override string? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            if (reader.IsEmptyElement)
            {
                return string.Empty;
            }

            string? first = null;
            StringBuilder? more = null;
            while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
            {
                if (reader.TokenType is not (XmlTokenType.Text or XmlTokenType.CData))
                {
                    continue;
                }

                var part = reader.GetString();
                if (first is null)
                {
                    first = part;
                }
                else
                {
                    (more ??= new StringBuilder(first)).Append(part);
                }
            }

            return more?.ToString() ?? first ?? string.Empty;
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, string value, XmlSerializerOptions options)
        {
            writer.WriteCData(value);
        }

        #endregion

    }

}
