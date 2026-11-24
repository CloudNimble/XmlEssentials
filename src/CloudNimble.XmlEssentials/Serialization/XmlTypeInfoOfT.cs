using System;
using System.Runtime.CompilerServices;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Provides XML serialization-related metadata about a type.
    /// </summary>
    /// <typeparam name="T">
    /// The type that the metadata describes.
    /// </typeparam>
    /// <remarks>
    /// Instances are created by the library. This class cannot be derived from outside the library.
    /// </remarks>
    public abstract class XmlTypeInfo<T> : XmlTypeInfo
    {

        #region Fields

        private readonly bool _handleNull;
        private readonly XmlValueConverter<T>? _valueConverter;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the converter that reads and writes values of type <typeparamref name="T"/>.
        /// </summary>
        /// <value>
        /// The converter associated with the type.
        /// </value>
        public new XmlConverter<T> Converter { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates type info.
        /// </summary>
        /// <param name="converter">The converter.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="rootName">The document element name.</param>
        private protected XmlTypeInfo(XmlConverter<T> converter, XmlSerializerOptions options, XmlName rootName)
            : base(typeof(T), converter, options, rootName, default(T) is null)
        {
            Converter = converter;
            _valueConverter = converter as XmlValueConverter<T>;
            _handleNull = converter.HandleNull;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns whether the element <paramref name="reader"/> is on is nil and the serializer, not the
        /// converter, is the one to handle that.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <returns><see langword="true"/> for an element marked <c>xsi:nil="true"</c> when <see cref="XmlConverter{T}.HandleNull"/> is <see langword="false"/>.</returns>
        /// <exception cref="XmlException">The element is nil and <typeparamref name="T"/> cannot be null.</exception>
        internal bool IsNil(ref Utf8XmlReader reader)
        {
            if (!reader.HasAttributes || _handleNull || !reader.GetNil())
            {
                return false;
            }

            return IsNillable
                ? true
                : throw XmlException.Create($"The element is nil but type '{typeof(T)}' is not nillable.");
        }

        /// <summary>
        /// Reads the element <paramref name="reader"/> is on, whatever its name.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <returns>The value, or <see langword="default"/> for a nil element.</returns>
        /// <remarks>
        /// <c>xsi:nil</c> is handled here once, so converters with <see cref="XmlConverter{T}.HandleNull"/>
        /// <see langword="false"/> never see a nil element.
        /// </remarks>
        internal T? ReadElement(ref Utf8XmlReader reader)
        {
            if (IsNil(ref reader))
            {
                reader.Skip();
                return default;
            }

            return Converter.Read(ref reader, typeof(T), Options);
        }

        /// <summary>
        /// Reads the document element.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The value.</returns>
        internal virtual T? ReadRoot(ref Utf8XmlReader reader)
        {
            XmlConverterHelpers.MoveToStartElement(ref reader);
            CheckRoot(ref reader);
            return ReadElement(ref reader);
        }

        /// <inheritdoc />
        internal sealed override object? ReadElementAsObject(ref Utf8XmlReader reader)
        {
            return ReadElement(ref reader);
        }

        /// <inheritdoc />
        internal sealed override object? ReadRootAsObject(ref Utf8XmlReader reader)
        {
            return ReadRoot(ref reader);
        }

        /// <summary>
        /// Reads the element <paramref name="reader"/> is on if all of it is in the reader's current block.
        /// For a reader that is not on its final block.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <param name="value">When this method returns <see langword="true"/>, the value, or <see langword="default"/> for a nil element.</param>
        /// <returns>
        /// <see langword="false"/> when more data is required. The reader may then be part way through the
        /// element; the caller restores it from a copy taken before the element.
        /// </returns>
        /// <remarks>
        /// A built-in value converter reads the element once and reports when it ran out of data. Any other
        /// converter cannot stop part way, so the element is skipped first to prove all of it has arrived and
        /// then read from a copy of the reader, the read-ahead <c>System.Text.Json</c> does for converters
        /// that cannot resume.
        /// </remarks>
        internal bool TryReadElement(ref Utf8XmlReader reader, out T? value)
        {
            value = default;
            if (IsNil(ref reader))
            {
                return reader.TrySkip();
            }

            return _valueConverter is not null
                ? _valueConverter.TryRead(ref reader, Options, out value)
                : TryReadWithReadAhead(ref reader, out value);
        }

        /// <summary>
        /// Reads the element with a converter that cannot stop part way: skips it to prove all of it is here,
        /// goes back, and reads it.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <param name="value">When this method returns <see langword="true"/>, the value.</param>
        /// <returns><see langword="false"/> when more data is required. The reader is still on the start tag.</returns>
        /// <remarks>
        /// Kept out of <see cref="TryReadElement"/> on purpose: the copy of the reader is a large local, and a
        /// method that declares one clears it on every call, whether or not this path runs.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal bool TryReadWithReadAhead(ref Utf8XmlReader reader, out T? value)
        {
            var start = reader;
            if (!reader.TrySkip())
            {
                value = default;
                return false;
            }

            reader = start;
            value = Converter.Read(ref reader, typeof(T), Options);
            return true;
        }

        /// <summary>
        /// Writes <paramref name="value"/> as the document element.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        internal virtual void WriteRoot(Utf8XmlWriter writer, T value)
        {
            if (XmlConverterHelpers.IsNull(value))
            {
                WriteNullRoot(writer);
                return;
            }

            if (_valueConverter is not null && TryWriteLeafStart(writer) is { } closeTag)
            {
                _valueConverter.WriteValue(writer, value, Options);
                writer.WriteLeafEnd(closeTag);
                return;
            }

            WriteStartRoot(writer);
            Converter.Write(writer, value, Options);
            writer.WriteEndElement();
        }

        /// <inheritdoc />
        internal sealed override void WriteContentAsObject(Utf8XmlWriter writer, object value)
        {
            Converter.Write(writer, (T)value, Options);
        }

        /// <inheritdoc />
        internal sealed override void WriteRootAsObject(Utf8XmlWriter writer, object? value)
        {
            if (value is null)
            {
                WriteNullRoot(writer);
                return;
            }

            WriteRoot(writer, (T)value);
        }

        #endregion

    }

}
