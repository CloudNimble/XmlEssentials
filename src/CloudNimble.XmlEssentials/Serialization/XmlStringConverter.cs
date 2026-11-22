using System;
using System.Text;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="string"/>. Whitespace is kept as written.
    /// </summary>
    internal sealed class XmlStringConverter : XmlValueConverter<string>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlStringConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override string Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return Encoding.UTF8.GetString(utf8);
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, string value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        #endregion

    }

}
