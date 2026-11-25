using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="Version"/> as <c>major.minor[.build[.revision]]</c>.
    /// </summary>
    internal sealed class XmlVersionConverter : XmlValueConverter<Version>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlVersionConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override Version Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return Version.TryParse(XmlLexical.Collapse(utf8), out var value)
                ? value
                : throw XmlException.Create("The value is not a valid version.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, Version value, XmlSerializerOptions options)
        {
            Span<byte> utf8 = stackalloc byte[43];
            value.TryFormat(utf8, out var written);
            writer.WriteStringValue(utf8[..written]);
        }

        #endregion

    }

}
