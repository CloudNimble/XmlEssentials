using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="bool"/> as <c>xs:boolean</c>.
    /// </summary>
    internal sealed class XmlBooleanConverter : XmlFormattedValueConverter<bool>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlBooleanConverter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 5;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override bool Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseBoolean(utf8, out var value) ? value : throw XmlException.Create("The value is not a boolean.");
        }

        /// <inheritdoc />
        internal override int Format(bool value, Span<byte> destination, XmlSerializerOptions options)
        {
            var text = value ? "true"u8 : "false"u8;
            text.CopyTo(destination);
            return text.Length;
        }

        #endregion

    }

}
