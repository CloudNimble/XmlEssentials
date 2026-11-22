using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="sbyte"/> as <c>xs:byte</c>.
    /// </summary>
    internal sealed class XmlSByteConverter : XmlValueConverter<sbyte>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlSByteConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override sbyte Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseSByte(utf8, out var value) ? value : throw new XmlException("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, sbyte value, XmlSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        #endregion

    }

}
