using System;
using System.Globalization;
using System.Numerics;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// A built-in converter for a scalar whose text has a known longest length and never needs escaping, such as
    /// a number, a date, or a GUID.
    /// </summary>
    /// <typeparam name="T">The CLR type.</typeparam>
    /// <remarks>
    /// Such a value is formatted straight into the output. As the only content of an element whose tags were
    /// built ahead of time, it is written with them in one reservation: the start tag, the value, and the end tag.
    /// </remarks>
    internal abstract class XmlFormattedValueConverter<T> : XmlValueConverter<T>
    {

        #region Properties

        /// <summary>
        /// Gets the most bytes <see cref="Format"/> writes.
        /// </summary>
        internal abstract int MaxBytes { get; }

        #endregion

        #region Private Methods

        /// <summary>
        /// Formats <paramref name="value"/> as its XML Schema lexical form.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="destination">Where the text goes. It holds at least <see cref="MaxBytes"/> bytes.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The number of bytes written.</returns>
        internal abstract int Format(T value, Span<byte> destination, XmlSerializerOptions options);

        /// <summary>
        /// Formats a floating-point number: <c>NaN</c>, <c>INF</c> and <c>-INF</c> for the values that are not
        /// finite, and otherwise the shortest text that reads back as the same number.
        /// </summary>
        /// <typeparam name="TFloat">The floating-point type.</typeparam>
        /// <param name="value">The number.</param>
        /// <param name="destination">Where the text goes.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The number of bytes written.</returns>
        /// <exception cref="XmlException">The number is not finite and <see cref="XmlSerializerOptions.NumberHandling"/> is <see cref="XmlNumberHandling.Strict"/>.</exception>
        internal static int FormatFloat<TFloat>(TFloat value, Span<byte> destination, XmlSerializerOptions options)
            where TFloat : struct, IFloatingPointIeee754<TFloat>
        {
            if (TFloat.IsFinite(value))
            {
                return FormatNumber(value, destination);
            }

            if (options.NumberHandling is XmlNumberHandling.Strict)
            {
                throw XmlException.Create("Named floating-point literals are not allowed when NumberHandling is Strict.");
            }

            var text = TFloat.IsNaN(value) ? "NaN"u8 : TFloat.IsPositiveInfinity(value) ? "INF"u8 : "-INF"u8;
            text.CopyTo(destination);
            return text.Length;
        }

        /// <summary>
        /// Formats a number with the invariant culture.
        /// </summary>
        /// <typeparam name="TNumber">The number type.</typeparam>
        /// <param name="value">The number.</param>
        /// <param name="destination">Where the text goes.</param>
        /// <returns>The number of bytes written.</returns>
        internal static int FormatNumber<TNumber>(TNumber value, Span<byte> destination)
            where TNumber : struct, IUtf8SpanFormattable
        {
            value.TryFormat(destination, out var written, default, CultureInfo.InvariantCulture);
            return written;
        }

        /// <summary>
        /// Writes <paramref name="value"/> as the only content of an element with no prefix, in no stated
        /// namespace, in one reservation, when the name allows it.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="localName">The local name of the element.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>
        /// <see langword="true"/> if the element was written; <see langword="false"/> if the name is not a
        /// short ASCII NCName, in which case nothing has been written.
        /// </returns>
        internal bool TryWriteElement(Utf8XmlWriter writer, ReadOnlySpan<char> localName, T value, XmlSerializerOptions options)
        {
            var destination = writer.BeginElementValue(localName, MaxBytes);
            if (destination.IsEmpty)
            {
                return false;
            }

            writer.EndElementValue(localName, destination, Format(value, destination, options));
            return true;
        }

        /// <inheritdoc />
        internal sealed override void WriteAttribute(Utf8XmlWriter writer, ReadOnlySpan<byte> local, ReadOnlySpan<byte> head, T value, XmlSerializerOptions options)
        {
            var destination = writer.BeginAttribute(local, head, MaxBytes);
            writer.EndAttribute(destination, Format(value, destination, options));
        }

        /// <inheritdoc />
        internal sealed override void WriteLeaf(Utf8XmlWriter writer, ReadOnlySpan<byte> openTag, ReadOnlySpan<byte> closeTag, T value, XmlSerializerOptions options)
        {
            var destination = writer.BeginLeaf(openTag, MaxBytes + closeTag.Length);
            writer.EndLeaf(destination, Format(value, destination, options), closeTag);
        }

        /// <inheritdoc />
        internal sealed override void WriteValue(Utf8XmlWriter writer, T value, XmlSerializerOptions options)
        {
            var destination = writer.BeginPlainValue(MaxBytes);
            writer.Advance(Format(value, destination, options));
        }

        #endregion

    }

}
