using System;
using System.Buffers;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Shared helpers for built-in converters and <see cref="XmlSerializer"/>.
    /// </summary>
    internal static class XmlConverterHelpers
    {

        #region Private Methods

        /// <summary>
        /// Advances to the next start tag, skipping the declaration, comments, and processing instructions.
        /// </summary>
        /// <param name="reader">The reader.</param>
        internal static void MoveToStartElement(ref Utf8XmlReader reader)
        {
            if (!TryMoveToStartElement(ref reader))
            {
                throw new XmlException("Expected a start element.");
            }
        }

        /// <summary>
        /// Reads the character content of the element <paramref name="reader"/> is on and leaves the reader on
        /// its end tag.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <param name="rented">
        /// Set when the content had to be copied; the caller returns it to <see cref="ArrayPool{T}.Shared"/>
        /// after using the result.
        /// </param>
        /// <param name="content">The content: every text and CDATA section joined, with comments and processing instructions left out.</param>
        /// <returns>
        /// <see langword="false"/> when the reader's block ended before the end tag, which can only happen
        /// when the reader is not on its final block. The reader is then part way through the element.
        /// </returns>
        /// <remarks>
        /// The usual element holds one run of text that needs no decoding, and that run is returned as a slice
        /// of the input. Only content split by a comment, a processing instruction, or a CDATA boundary, or
        /// holding a character or entity reference, is copied.
        /// </remarks>
        internal static bool TryReadContent(ref Utf8XmlReader reader, ref byte[]? rented, out ReadOnlySpan<byte> content)
        {
            content = default;
            if (reader.IsEmptyElement)
            {
                return true;
            }

            var length = 0;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case XmlTokenType.EndElement:
                        content = rented is null ? content : rented.AsSpan(0, length);
                        return true;
                    case XmlTokenType.Text:
                    case XmlTokenType.CData:
                        var segment = reader.ValueSpan;
                        if (rented is null && content.IsEmpty && !reader.ValueIsEscaped)
                        {
                            content = segment;
                            break;
                        }

                        if (rented is null || rented.Length - length < segment.Length)
                        {
                            var grown = ArrayPool<byte>.Shared.Rent(Math.Max(256, (content.Length + length + segment.Length) * 2));
                            if (rented is null)
                            {
                                content.CopyTo(grown);
                                length = content.Length;
                            }
                            else
                            {
                                rented.AsSpan(0, length).CopyTo(grown);
                                ArrayPool<byte>.Shared.Return(rented);
                            }

                            rented = grown;
                        }

                        segment.CopyTo(rented.AsSpan(length));
                        length += segment.Length;
                        break;
                    case XmlTokenType.Comment:
                    case XmlTokenType.ProcessingInstruction:
                        break;
                    default:
                        throw new XmlException($"Unexpected {reader.TokenType} in simple content.");
                }
            }

            return false;
        }

        /// <summary>
        /// Advances to the next start tag if one exists.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns><see langword="true"/> when the reader is on a start tag.</returns>
        internal static bool TryMoveToStartElement(ref Utf8XmlReader reader)
        {
            while (reader.TokenType is not XmlTokenType.StartElement)
            {
                if (!reader.Read())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Writes an attribute start tag for <paramref name="name"/>.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="name">The qualified name.</param>
        internal static void WriteStartAttribute(Utf8XmlWriter writer, XmlName name)
        {
            if (name.Prefix.Length > 0)
            {
                writer.WriteStartAttribute(name.Prefix, name.LocalName, name.NamespaceUri);
                return;
            }

            writer.WriteStartAttribute(name.LocalName);
        }

        #endregion

    }

}
