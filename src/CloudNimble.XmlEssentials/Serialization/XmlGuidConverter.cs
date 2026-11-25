using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="Guid"/> in the hyphenated form.
    /// </summary>
    internal sealed class XmlGuidConverter : XmlFormattedValueConverter<Guid>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlGuidConverter Instance = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        internal override int MaxBytes => 36;

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override Guid Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return XmlLexical.TryParseGuid(utf8, out var value) ? value : throw XmlException.Create("The value is not a valid GUID.");
        }

        /// <inheritdoc />
        internal override int Format(Guid value, Span<byte> destination, XmlSerializerOptions options)
        {
            value.TryFormat(destination, out var written, "D");
            return written;
        }

        #endregion

    }

}
