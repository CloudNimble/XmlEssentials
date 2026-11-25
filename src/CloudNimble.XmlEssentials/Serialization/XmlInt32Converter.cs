using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="int"/> as <c>xs:int</c>.
    /// </summary>
    internal sealed class XmlInt32Converter : XmlFormattedValueConverter<int>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlInt32Converter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 11;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override int Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseInt32(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid integer.");
        }

        /// <inheritdoc />
        internal override int Format(int value, Span<byte> destination, XmlSerializerOptions options)
        {
            return FormatNumber(value, destination);
        }

        #endregion

    }

}
