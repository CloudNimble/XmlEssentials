using System;
using System.Buffers;
using System.Text;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts <see cref="char"/> as a single UTF-16 code unit.
    /// </summary>
    internal sealed class XmlCharConverter : XmlValueConverter<char>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlCharConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override char Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            // A value that is one character is that character, white space included: a blank or a tab is a
            // char like any other, and collapsing it away would leave nothing to read. White space is
            // collapsed only around a character that is not itself white space.
            var collapsed = utf8.Length == 1 ? utf8 : XmlLexical.Collapse(utf8);
            var status = Rune.DecodeFromUtf8(collapsed, out var rune, out var consumed);
            if (status is not OperationStatus.Done || consumed != collapsed.Length || !rune.IsBmp)
            {
                throw XmlException.Create("The value is not a single character.");
            }

            return (char)rune.Value;
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, char value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(new ReadOnlySpan<char>(in value));
        }

        #endregion

    }

}
