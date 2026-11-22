using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="int"/> as <c>xs:int</c>.
    /// </summary>
    internal sealed class XmlInt32Converter : XmlValueConverter<int>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlInt32Converter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override int Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseInt32(utf8, out var value) ? value : throw new XmlException("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, int value, XmlSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        #endregion

    }

}
