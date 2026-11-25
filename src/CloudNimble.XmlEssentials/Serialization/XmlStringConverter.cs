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
        internal override void WriteAttribute(Utf8XmlWriter writer, ReadOnlySpan<byte> local, ReadOnlySpan<byte> head, string value, XmlSerializerOptions options)
        {
            if (!writer.TryWritePlainAttribute(local, head, value))
            {
                base.WriteAttribute(writer, local, head, value, options);
            }
        }

        /// <inheritdoc />
        internal override void WriteLeaf(Utf8XmlWriter writer, ReadOnlySpan<byte> openTag, ReadOnlySpan<byte> closeTag, string value, XmlSerializerOptions options)
        {
            if (!writer.TryWritePlainLeaf(openTag, closeTag, value))
            {
                base.WriteLeaf(writer, openTag, closeTag, value, options);
            }
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, string value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        #endregion

    }

}
