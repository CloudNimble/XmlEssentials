using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Shared helpers for built-in converters and <see cref="XmlSerializer"/>.
    /// </summary>
    internal static class XmlConverterHelpers
    {

        #region Private Methods

        /// <summary>
        /// Returns whether <paramref name="value"/> is null, without boxing a <see cref="Nullable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns><see langword="true"/> for a null reference or a <see cref="Nullable{T}"/> with no value.</returns>
        /// <remarks>
        /// <c>value is null</c> on a type parameter is compiled as a box and a null test. The JIT usually removes the
        /// box for a <see cref="Nullable{T}"/>, but not always: with profile-guided inlining it stayed, and writing a
        /// list of 1,000 <c>int?</c> allocated 24,000 bytes. The comparer never boxes.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsNull<T>([NotNullWhen(false)] T? value)
        {
            return typeof(T).IsValueType
                ? default(T) is null && EqualityComparer<T>.Default.Equals(value, default!)
                : value is null;
        }

        /// <summary>
        /// Copies <paramref name="segment"/> to the end of the pooled buffer, renting or growing it first.
        /// </summary>
        /// <param name="segment">The run to add.</param>
        /// <param name="rented">The pooled buffer, or null when nothing has been copied yet.</param>
        /// <param name="length">The number of bytes used in <paramref name="rented"/>.</param>
        /// <param name="content">The run held as a slice of the input, copied in first when the buffer is rented.</param>
        internal static void CopyContent(ReadOnlySpan<byte> segment, ref byte[]? rented, ref int length, ReadOnlySpan<byte> content)
        {
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
        }

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

            if (reader.TryReadLeafContent(out content))
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
                        if (rented is null && content.IsEmpty && (!reader.ValueIsEscaped || reader.IsEndTagNext))
                        {
                            content = reader.ValueSpan;
                        }
                        else
                        {
                            CopyContent(reader.ValueSpan, ref rented, ref length, content);
                        }

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
            if (name.Prefix.Length > 0 || name.NamespaceUri.Length > 0)
            {
                writer.WriteStartAttribute(name.Prefix.Length > 0 ? name.Prefix : null, name.LocalName, name.NamespaceUri);
                return;
            }

            writer.WriteStartAttribute(name.LocalName);
        }

        /// <summary>
        /// Reads the derived-type discriminator from attributes on the current start tag.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <param name="placement">Where the discriminator is written.</param>
        /// <param name="utf8AttributeName">The discriminator attribute local name when placement is attribute.</param>
        /// <returns>The discriminator, or null when it is absent.</returns>
        internal static string? ReadDiscriminator(ref Utf8XmlReader reader, XmlDiscriminatorPlacement placement, byte[] utf8AttributeName)
        {
            if (!reader.HasAttributes)
            {
                return null;
            }

            string? value = null;
            while (reader.MoveToNextAttribute())
            {
                if (placement is XmlDiscriminatorPlacement.XsiType)
                {
                    if (reader.LocalNameEquals(XmlAttributes.Type) && reader.NamespaceUriEquals(XmlNamespaces.Xsi))
                    {
                        value = reader.GetString();
                    }

                    continue;
                }

                if (utf8AttributeName.Length > 0 && reader.LocalNameEquals(utf8AttributeName) && reader.PrefixSpan.IsEmpty)
                {
                    value = reader.GetString();
                }
            }

            reader.MoveToElement();
            return value;
        }

        /// <summary>
        /// Writes the derived-type discriminator on the open start tag.
        /// </summary>
        /// <param name="writer">The writer, on an open start tag.</param>
        /// <param name="placement">Where the discriminator is written.</param>
        /// <param name="utf8AttributeName">The discriminator attribute local name when placement is attribute.</param>
        /// <param name="discriminator">The discriminator value.</param>
        internal static void WriteDiscriminator(Utf8XmlWriter writer, XmlDiscriminatorPlacement placement, byte[] utf8AttributeName, string discriminator)
        {
            if (placement is XmlDiscriminatorPlacement.Attribute)
            {
                writer.WriteStartAttributeUtf8(utf8AttributeName, [], []);
                writer.WriteStringValue(discriminator);
                writer.WriteEndAttribute();
                return;
            }

            if (placement is XmlDiscriminatorPlacement.XsiType)
            {
                writer.WriteStartAttribute("xsi", XmlAttributes.Type, XmlNamespaces.Xsi);
                writer.WriteStringValue(discriminator);
                writer.WriteEndAttribute();
            }
        }

        #endregion

    }

}
