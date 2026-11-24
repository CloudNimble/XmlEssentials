using System;
using System.Globalization;
using System.Numerics;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="BigInteger"/> as <c>xs:integer</c>.
    /// </summary>
    internal sealed class XmlBigIntegerConverter : XmlValueConverter<BigInteger>
    {

        #region Fields

        internal static readonly XmlBigIntegerConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override BigInteger Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseBigInteger(utf8, out var value) ? value : throw new XmlException("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, BigInteger value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
        }

        #endregion

    }

}
