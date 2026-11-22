using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="bool"/> as <c>xs:boolean</c>.
    /// </summary>
    internal sealed class XmlBooleanConverter : XmlValueConverter<bool>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlBooleanConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override bool Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseBoolean(utf8, out var value) ? value : throw new XmlException("The value is not a boolean.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, bool value, XmlSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }

        #endregion

    }

}
