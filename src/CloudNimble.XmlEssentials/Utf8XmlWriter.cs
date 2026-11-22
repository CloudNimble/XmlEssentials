using System;
using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// A UTF-8 XML writer analogous to <c>Utf8JsonWriter</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// var buffer = new ArrayBufferWriter&lt;byte&gt;();
    /// using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true }))
    /// {
    ///     writer.WriteStartElement("root");
    ///     writer.WriteAttributeString("id", "1");
    ///     writer.WriteElementNumber("n", 42);
    ///     writer.WriteEndElement();
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Destinations are <see cref="IBufferWriter{T}"/> or <see cref="Stream"/>. Names and values are UTF-8.
    /// A max-* of 0 on <see cref="XmlWriterOptions"/> uses the DoS table default. Dispose flushes and throws
    /// when elements are still open unless <see cref="XmlWriterOptions.SkipValidation"/> is true.
    /// </remarks>
    public sealed class Utf8XmlWriter : IDisposable, IAsyncDisposable
    {

        #region Fields

        /// <summary>
        /// The number of distinct namespace prefixes and URIs a writer keeps for reuse.
        /// </summary>
        internal const int MaxInternedNamespaceValues = 64;

        private int _autoNs;
        private XmlWriterName8 _attrLocalsInline;
        private XmlWriterName[]? _attrLocalsHeap;
        private XmlWriterName8 _attrNsInline;
        private XmlWriterName[]? _attrNsHeap;
        private int _buffered;
        private byte[] _buffer;
        private IBufferWriter<byte>? _bufferWriter;
        private long _committed;
        private int _depth;
        private bool _disposed;
        private bool _emittedBom;
        private XmlWriterFrame[]? _frames;
        private XmlWriterFrame8 _framesInline;
        private bool _inAttribute;
        private int _indentSize;
        private int _maxAttributes;
        private int _maxDepth;
        private int _maxNameBytes;
        private Memory<byte> _memory;
        private byte[] _names;
        private int _namesLength;
        private byte[]? _newLineUtf8;
        private Int8Buffer _nsCacheIndexes;
        private int _nsCacheMask;
        private int _nsCount;
        private XmlWriterNsBinding[]? _nsHeap;
        private byte[][]? _nsInterned;
        private int _nsInternedCount;
        private XmlWriterOptions _options;
        private bool _seenRoot;
        private Stream? _stream;
        private XmlVersion _version;
        private bool _wroteDeclaration;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the number of bytes flushed to the output.
        /// </summary>
        public long BytesCommitted => _committed;

        /// <summary>
        /// Gets the number of bytes buffered and not yet flushed.
        /// </summary>
        public int BytesPending => _buffered;

        /// <summary>
        /// Gets the current element depth. The root is 1 while its start tag is open or it has content.
        /// </summary>
        public int CurrentDepth => _depth;

        /// <summary>
        /// Gets the options used to create the writer.
        /// </summary>
        public XmlWriterOptions Options => _options;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a writer that appends UTF-8 XML to <paramref name="bufferWriter"/>.
        /// </summary>
        /// <param name="bufferWriter">The destination.</param>
        /// <param name="options">Writer options.</param>
        public Utf8XmlWriter(IBufferWriter<byte> bufferWriter, XmlWriterOptions options = default)
        {
            ArgumentNullException.ThrowIfNull(bufferWriter);
            _bufferWriter = bufferWriter;
            _buffer = [];
            _names = [];
            Configure(in options);
        }

        /// <summary>
        /// Creates a writer that writes UTF-8 XML to <paramref name="utf8Xml"/>.
        /// </summary>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="options">Writer options.</param>
        public Utf8XmlWriter(Stream utf8Xml, XmlWriterOptions options = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            _stream = utf8Xml;
            _buffer = ArrayPool<byte>.Shared.Rent(XmlLimits.DefaultBufferSize);
            _memory = _buffer;
            _names = [];
            Configure(in options);
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                if (!_options.SkipValidation && _depth > 0)
                {
                    throw new XmlException("The document has unclosed elements.");
                }

                Flush();
            }
            finally
            {
                ReturnBuffer();
                _disposed = true;
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                if (!_options.SkipValidation && _depth > 0)
                {
                    throw new XmlException("The document has unclosed elements.");
                }

                await FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                ReturnBuffer();
                _disposed = true;
            }
        }

        /// <summary>
        /// Flushes buffered bytes to the destination.
        /// </summary>
        public void Flush()
        {
            ThrowIfDisposed();
            FlushBuffer();
            _stream?.Flush();
        }

        /// <summary>
        /// Flushes buffered bytes to the destination asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the flush finishes.</returns>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (_buffered > 0)
            {
                if (_stream is not null)
                {
                    await _stream.WriteAsync(_buffer.AsMemory(0, _buffered), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _bufferWriter!.Advance(_buffered);
                }

                _committed += _buffered;
                _buffered = 0;
                _memory = _stream is not null ? _buffer : default;
            }

            if (_stream is not null)
            {
                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Resets the writer to start a new document on the same destination.
        /// </summary>
        public void Reset()
        {
            ThrowIfDisposed();
            FlushBuffer();
            _autoNs = 0;
            _committed = 0;
            _depth = 0;
            _emittedBom = false;
            _inAttribute = false;
            _nsCacheMask = 0;
            _nsCount = 0;
            _seenRoot = false;
            _wroteDeclaration = false;
            _namesLength = 0;
            _memory = _stream is not null ? _buffer : default;
        }

        /// <summary>
        /// Resets the writer onto a new <see cref="IBufferWriter{T}"/> destination.
        /// </summary>
        /// <param name="bufferWriter">The new destination.</param>
        public void Reset(IBufferWriter<byte> bufferWriter)
        {
            ArgumentNullException.ThrowIfNull(bufferWriter);
            Reset();
            _bufferWriter = bufferWriter;
            _stream = null;
            _memory = default;
        }

        /// <summary>
        /// Resets the writer onto a new <see cref="Stream"/> destination.
        /// </summary>
        /// <param name="utf8Xml">The new destination.</param>
        public void Reset(Stream utf8Xml)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            Reset();
            _stream = utf8Xml;
            _bufferWriter = null;
            if (_buffer.Length == 0)
            {
                _buffer = ArrayPool<byte>.Shared.Rent(XmlLimits.DefaultBufferSize);
            }

            _memory = _buffer;
        }

        /// <summary>
        /// Writes <c>xsi:nil="true"</c> on the current start tag.
        /// </summary>
        public void WriteNilValue()
        {
            ThrowIfDisposed();
            if (_depth == 0 || !CurrentFrame().TagOpen)
            {
                throw new XmlException("xsi:nil must be written on a start tag.");
            }

            EnsureXsi();
            TrackAttribute("nil"u8, XmlUtf8.Xsi);
            WriteRaw(" xsi:nil=\"true\""u8);
        }

        /// <summary>
        /// Writes an empty nillable element.
        /// </summary>
        /// <param name="localName">The local name.</param>
        public void WriteNilElement(string localName)
        {
            WriteStartElement(localName);
            WriteNilValue();
            WriteEndElement();
        }

        /// <summary>
        /// Writes an empty nillable element in <paramref name="namespaceUri"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        public void WriteNilElement(string localName, string? namespaceUri)
        {
            WriteStartElement(localName, namespaceUri);
            WriteNilValue();
            WriteEndElement();
        }

        /// <summary>
        /// Writes an empty nillable element.
        /// </summary>
        /// <param name="localName">The local name.</param>
        public void WriteNilElement(ReadOnlySpan<char> localName)
        {
            WriteStartElement(localName);
            WriteNilValue();
            WriteEndElement();
        }

        /// <summary>
        /// Writes an empty nillable element.
        /// </summary>
        /// <param name="utf8LocalName">The UTF-8 local name.</param>
        public void WriteNilElement(ReadOnlySpan<byte> utf8LocalName)
        {
            WriteStartElement(utf8LocalName);
            WriteNilValue();
            WriteEndElement();
        }

        /// <summary>
        /// Writes an empty element <c>&lt;localName/&gt;</c>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        public void WriteEmptyElement(string localName)
        {
            ArgumentNullException.ThrowIfNull(localName);
            WriteStartElementLiteral(localName, prefix: null, namespaceUri: null, empty: true);
        }

        /// <summary>
        /// Writes an empty element in <paramref name="namespaceUri"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        public void WriteEmptyElement(string localName, string? namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(localName);
            WriteEmptyElementChars(localName, default, namespaceUri.AsSpan());
        }

        /// <summary>
        /// Writes an empty element with an explicit prefix.
        /// </summary>
        /// <param name="prefix">The prefix, or null.</param>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        public void WriteEmptyElement(string? prefix, string localName, string? namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(localName);
            WriteEmptyElementChars(localName, prefix.AsSpan(), namespaceUri.AsSpan());
        }

        /// <summary>
        /// Writes an empty element <c>&lt;localName/&gt;</c>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        public void WriteEmptyElement(ReadOnlySpan<char> localName)
        {
            WriteEmptyElementChars(localName, default, default);
        }

        /// <summary>
        /// Writes an empty element in <paramref name="namespaceUri"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        public void WriteEmptyElement(ReadOnlySpan<char> localName, ReadOnlySpan<char> namespaceUri)
        {
            WriteEmptyElementChars(localName, default, namespaceUri);
        }

        /// <summary>
        /// Writes an empty UTF-8 element.
        /// </summary>
        /// <param name="utf8LocalName">The UTF-8 local name.</param>
        public void WriteEmptyElement(ReadOnlySpan<byte> utf8LocalName)
        {
            WriteStartElementUtf8(utf8LocalName, [], [], empty: true);
        }

        /// <summary>
        /// Writes an empty UTF-8 element in <paramref name="utf8NamespaceUri"/>.
        /// </summary>
        /// <param name="utf8LocalName">The UTF-8 local name.</param>
        /// <param name="utf8NamespaceUri">The UTF-8 namespace URI.</param>
        public void WriteEmptyElement(ReadOnlySpan<byte> utf8LocalName, ReadOnlySpan<byte> utf8NamespaceUri)
        {
            WriteStartElementUtf8(utf8LocalName, [], utf8NamespaceUri, empty: true);
        }

        /// <summary>
        /// Writes an empty UTF-8 element with an explicit prefix.
        /// </summary>
        /// <param name="utf8Prefix">The UTF-8 prefix.</param>
        /// <param name="utf8LocalName">The UTF-8 local name.</param>
        /// <param name="utf8NamespaceUri">The UTF-8 namespace URI.</param>
        public void WriteEmptyElement(ReadOnlySpan<byte> utf8Prefix, ReadOnlySpan<byte> utf8LocalName, ReadOnlySpan<byte> utf8NamespaceUri)
        {
            WriteStartElementUtf8(utf8LocalName, utf8Prefix, utf8NamespaceUri, empty: true);
        }

        /// <summary>
        /// Writes an empty element from cached <paramref name="localName"/>.
        /// </summary>
        /// <param name="localName">The encoded local name.</param>
        public void WriteEmptyElement(XmlEncodedText localName)
        {
            WriteStartElementUtf8(localName.EncodedUtf8Bytes, [], [], empty: true);
        }

        /// <summary>
        /// Writes the XML declaration.
        /// </summary>
        public void WriteXmlDeclaration()
        {
            ThrowIfDisposed();
            if (_wroteDeclaration || _seenRoot || _buffered > 0 || _committed > 0)
            {
                throw new XmlException("The XML declaration must be the first output.");
            }

            EmitDeclaration();
        }

        /// <summary>
        /// Writes a start tag with <paramref name="localName"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        public void WriteStartElement(string localName)
        {
            ArgumentNullException.ThrowIfNull(localName);
            WriteStartElementLiteral(localName, prefix: null, namespaceUri: null, empty: false);
        }

        /// <summary>
        /// Writes a start tag in <paramref name="namespaceUri"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        public void WriteStartElement(string localName, string? namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(localName);
            WriteStartElementLiteral(localName, prefix: null, namespaceUri, empty: false);
        }

        /// <summary>
        /// Writes a start tag with an explicit prefix.
        /// </summary>
        /// <param name="prefix">The prefix, or null.</param>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        public void WriteStartElement(string? prefix, string localName, string? namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(localName);
            WriteStartElementLiteral(localName, prefix, namespaceUri, empty: false);
        }

        /// <summary>
        /// Writes a start tag with <paramref name="localName"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        public void WriteStartElement(ReadOnlySpan<char> localName)
        {
            WriteStartElementChars(localName, default, default);
        }

        /// <summary>
        /// Writes a start tag with UTF-8 <paramref name="utf8LocalName"/>.
        /// </summary>
        /// <param name="utf8LocalName">The UTF-8 local name.</param>
        public void WriteStartElement(ReadOnlySpan<byte> utf8LocalName)
        {
            WriteStartElementUtf8(utf8LocalName, [], []);
        }

        /// <summary>
        /// Writes a start tag from cached <paramref name="localName"/>.
        /// </summary>
        /// <param name="localName">The encoded local name.</param>
        public void WriteStartElement(XmlEncodedText localName)
        {
            WriteStartElementUtf8(localName.EncodedUtf8Bytes, [], []);
        }

        /// <summary>
        /// Writes a start tag in <paramref name="namespaceUri"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        public void WriteStartElement(ReadOnlySpan<char> localName, ReadOnlySpan<char> namespaceUri)
        {
            WriteStartElementChars(localName, default, namespaceUri);
        }

        /// <summary>
        /// Writes a start tag in UTF-8 <paramref name="utf8NamespaceUri"/>.
        /// </summary>
        /// <param name="utf8LocalName">The UTF-8 local name.</param>
        /// <param name="utf8NamespaceUri">The UTF-8 namespace URI.</param>
        public void WriteStartElement(ReadOnlySpan<byte> utf8LocalName, ReadOnlySpan<byte> utf8NamespaceUri)
        {
            WriteStartElementUtf8(utf8LocalName, [], utf8NamespaceUri);
        }

        /// <summary>
        /// Writes a start tag with UTF-8 prefix, local name, and namespace.
        /// </summary>
        /// <param name="utf8Prefix">The UTF-8 prefix.</param>
        /// <param name="utf8LocalName">The UTF-8 local name.</param>
        /// <param name="utf8NamespaceUri">The UTF-8 namespace URI.</param>
        public void WriteStartElement(ReadOnlySpan<byte> utf8Prefix, ReadOnlySpan<byte> utf8LocalName, ReadOnlySpan<byte> utf8NamespaceUri)
        {
            WriteStartElementUtf8(utf8LocalName, utf8Prefix, utf8NamespaceUri);
        }

        /// <summary>
        /// Writes an end tag, using an empty-element closer when the start tag has no content.
        /// </summary>
        public void WriteEndElement()
        {
            ThrowIfDisposed();
            if (_inAttribute)
            {
                WriteEndAttribute();
            }

            if (_depth == 0)
            {
                throw new XmlException("No element to end.");
            }

            ref var frame = ref CurrentFrame();
            if (frame.TagOpen && !frame.HasContent)
            {
                WriteRaw("/>"u8);
            }
            else
            {
                CloseStartTag();
                if (_options.Indented && !frame.HasText)
                {
                    WriteIndent(_depth - 1);
                }

                WriteTagClose(frame);
            }

            PopNs(_depth);
            _depth--;
        }

        /// <summary>
        /// Writes a full end tag <c>&lt;/name&gt;</c> even when the element is empty.
        /// </summary>
        public void WriteFullEndElement()
        {
            ThrowIfDisposed();
            if (_inAttribute)
            {
                WriteEndAttribute();
            }

            if (_depth == 0)
            {
                throw new XmlException("No element to end.");
            }

            CloseStartTag();
            ref var frame = ref CurrentFrame();
            if (_options.Indented && frame.HasContent && !frame.HasText)
            {
                WriteIndent(_depth - 1);
            }

            WriteRaw("</"u8);
            WriteFrameName(frame);
            WriteRawByte((byte)'>');
            PopNs(_depth);
            _depth--;
        }

        /// <summary>
        /// Writes an element with a string value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value, or null for an empty element.</param>
        public void WriteElementString(string localName, string? value)
        {
            ArgumentNullException.ThrowIfNull(localName);
            WriteCompleteElement(localName, value, valueIsNull: value is null);
        }

        /// <summary>
        /// Writes an element in <paramref name="namespaceUri"/> with a string value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <param name="value">The value.</param>
        public void WriteElementString(string localName, string? namespaceUri, string? value)
        {
            ArgumentNullException.ThrowIfNull(localName);
            if (string.IsNullOrEmpty(namespaceUri))
            {
                WriteCompleteElement(localName, value, valueIsNull: value is null);
                return;
            }

            WriteStartElement(localName, namespaceUri);
            if (value is not null)
            {
                WriteStringValue(value);
            }

            WriteEndElement();
        }

        /// <summary>
        /// Writes an element with an XSD dateTime value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        public void WriteElementString(string localName, DateTime value)
        {
            ArgumentNullException.ThrowIfNull(localName);
            WriteStartElement(localName);
            WriteStringValue(value);
            WriteEndElement();
        }

        /// <summary>
        /// Writes an element with an XSD dateTime value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        public void WriteElementString(string localName, DateTimeOffset value)
        {
            WriteStartElement(localName);
            WriteStringValue(value);
            WriteEndElement();
        }

        /// <summary>
        /// Writes an element with a GUID value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        public void WriteElementString(string localName, Guid value)
        {
            WriteStartElement(localName);
            WriteStringValue(value);
            WriteEndElement();
        }

        /// <summary>
        /// Writes an element with a string value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        public void WriteElementString(ReadOnlySpan<char> localName, ReadOnlySpan<char> value)
        {
            WriteStartElement(localName);
            WriteStringValue(value);
            WriteEndElement();
        }

        /// <summary>
        /// Writes an element with a UTF-8 string value.
        /// </summary>
        /// <param name="utf8LocalName">The UTF-8 local name.</param>
        /// <param name="utf8Value">The UTF-8 value.</param>
        public void WriteElementString(ReadOnlySpan<byte> utf8LocalName, ReadOnlySpan<byte> utf8Value)
        {
            WriteCompleteElementUtf8(utf8LocalName, [], LookupUri([]), utf8Value);
        }

        /// <summary>
        /// Writes an element with an integer value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        public void WriteElementNumber(string localName, int value)
        {
            ArgumentNullException.ThrowIfNull(localName);
            Span<byte> buffer = stackalloc byte[16];
            Utf8Formatter.TryFormat(value, buffer, out var written);
            WriteCompleteElementUtf8Value(localName, buffer[..written]);
        }

        /// <summary>
        /// Writes an element with a 64-bit integer value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        public void WriteElementNumber(string localName, long value)
        {
            ArgumentNullException.ThrowIfNull(localName);
            Span<byte> buffer = stackalloc byte[24];
            Utf8Formatter.TryFormat(value, buffer, out var written);
            WriteCompleteElementUtf8Value(localName, buffer[..written]);
        }

        /// <summary>
        /// Writes an element with a double value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        public void WriteElementNumber(string localName, double value)
        {
            ArgumentNullException.ThrowIfNull(localName);
            if (double.IsNaN(value))
            {
                WriteCompleteElementUtf8Value(localName, "NaN"u8);
                return;
            }

            if (double.IsPositiveInfinity(value))
            {
                WriteCompleteElementUtf8Value(localName, "INF"u8);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                WriteCompleteElementUtf8Value(localName, "-INF"u8);
                return;
            }

            Span<byte> buffer = stackalloc byte[32];
            Utf8Formatter.TryFormat(value, buffer, out var written);
            WriteCompleteElementUtf8Value(localName, buffer[..written]);
        }

        /// <summary>
        /// Writes an element with a single value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        public void WriteElementNumber(string localName, float value)
        {
            ArgumentNullException.ThrowIfNull(localName);
            if (float.IsNaN(value))
            {
                WriteCompleteElementUtf8Value(localName, "NaN"u8);
                return;
            }

            if (float.IsPositiveInfinity(value))
            {
                WriteCompleteElementUtf8Value(localName, "INF"u8);
                return;
            }

            if (float.IsNegativeInfinity(value))
            {
                WriteCompleteElementUtf8Value(localName, "-INF"u8);
                return;
            }

            Span<byte> buffer = stackalloc byte[24];
            Utf8Formatter.TryFormat(value, buffer, out var written);
            WriteCompleteElementUtf8Value(localName, buffer[..written]);
        }

        /// <summary>
        /// Writes an element with a decimal value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        public void WriteElementNumber(string localName, decimal value)
        {
            ArgumentNullException.ThrowIfNull(localName);
            Span<byte> buffer = stackalloc byte[32];
            Utf8Formatter.TryFormat(value, buffer, out var written);
            WriteCompleteElementUtf8Value(localName, buffer[..written]);
        }

        /// <summary>
        /// Writes an element with an unsigned integer value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        [CLSCompliant(false)]
        public void WriteElementNumber(string localName, uint value)
        {
            ArgumentNullException.ThrowIfNull(localName);
            Span<byte> buffer = stackalloc byte[16];
            Utf8Formatter.TryFormat(value, buffer, out var written);
            WriteCompleteElementUtf8Value(localName, buffer[..written]);
        }

        /// <summary>
        /// Writes an element with an unsigned 64-bit integer value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        [CLSCompliant(false)]
        public void WriteElementNumber(string localName, ulong value)
        {
            ArgumentNullException.ThrowIfNull(localName);
            Span<byte> buffer = stackalloc byte[24];
            Utf8Formatter.TryFormat(value, buffer, out var written);
            WriteCompleteElementUtf8Value(localName, buffer[..written]);
        }

        /// <summary>
        /// Writes an element with a boolean value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        public void WriteElementBoolean(string localName, bool value)
        {
            WriteStartElement(localName);
            WriteBooleanValue(value);
            WriteEndElement();
        }

        /// <summary>
        /// Writes an element with a boolean value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        public void WriteElementBoolean(ReadOnlySpan<char> localName, bool value)
        {
            WriteStartElement(localName);
            WriteBooleanValue(value);
            WriteEndElement();
        }

        /// <summary>
        /// Writes an element with a boolean value.
        /// </summary>
        /// <param name="utf8LocalName">The UTF-8 local name.</param>
        /// <param name="value">The value.</param>
        public void WriteElementBoolean(ReadOnlySpan<byte> utf8LocalName, bool value)
        {
            WriteStartElement(utf8LocalName);
            WriteBooleanValue(value);
            WriteEndElement();
        }

        /// <summary>
        /// Writes an element with a base64 value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="bytes">The bytes to encode.</param>
        public void WriteElementBase64String(string localName, ReadOnlySpan<byte> bytes)
        {
            WriteStartElement(localName);
            WriteBase64StringValue(bytes);
            WriteEndElement();
        }

        /// <summary>
        /// Writes an element with a base64 value.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="bytes">The bytes to encode.</param>
        public void WriteElementBase64String(ReadOnlySpan<char> localName, ReadOnlySpan<byte> bytes)
        {
            WriteStartElement(localName);
            WriteBase64StringValue(bytes);
            WriteEndElement();
        }

        /// <summary>
        /// Writes an element with a base64 value.
        /// </summary>
        /// <param name="utf8LocalName">The UTF-8 local name.</param>
        /// <param name="bytes">The bytes to encode.</param>
        public void WriteElementBase64String(ReadOnlySpan<byte> utf8LocalName, ReadOnlySpan<byte> bytes)
        {
            WriteStartElement(utf8LocalName);
            WriteBase64StringValue(bytes);
            WriteEndElement();
        }

        /// <summary>
        /// Writes a complete attribute.
        /// </summary>
        /// <param name="name">The attribute name.</param>
        /// <param name="value">The value.</param>
        public void WriteAttributeString(string name, string? value)
        {
            WriteStartAttribute(name);
            if (value is not null)
            {
                WriteStringValue(value);
            }

            WriteEndAttribute();
        }

        /// <summary>
        /// Writes a complete attribute with an integer value.
        /// </summary>
        /// <param name="name">The attribute name.</param>
        /// <param name="value">The value.</param>
        public void WriteAttributeNumber(string name, int value)
        {
            ArgumentNullException.ThrowIfNull(name);
            WriteAttributeNumberLiteral(name, value);
        }

        /// <summary>
        /// Writes a complete attribute with a 64-bit integer value.
        /// </summary>
        /// <param name="name">The attribute name.</param>
        /// <param name="value">The value.</param>
        public void WriteAttributeNumber(string name, long value)
        {
            ArgumentNullException.ThrowIfNull(name);
            WriteStartAttribute(name);
            WriteNumberValue(value);
            WriteEndAttribute();
        }

        /// <summary>
        /// Writes a complete attribute with a double value.
        /// </summary>
        /// <param name="name">The attribute name.</param>
        /// <param name="value">The value.</param>
        public void WriteAttributeNumber(string name, double value)
        {
            ArgumentNullException.ThrowIfNull(name);
            WriteStartAttribute(name);
            WriteNumberValue(value);
            WriteEndAttribute();
        }

        /// <summary>
        /// Writes a complete attribute in <paramref name="namespaceUri"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <param name="value">The value.</param>
        public void WriteAttributeString(string localName, string? namespaceUri, string? value)
        {
            WriteStartAttribute(null, localName, namespaceUri);
            if (value is not null)
            {
                WriteStringValue(value);
            }

            WriteEndAttribute();
        }

        /// <summary>
        /// Writes a complete attribute with an explicit prefix.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <param name="value">The value.</param>
        public void WriteAttributeString(string? prefix, string localName, string? namespaceUri, string? value)
        {
            WriteStartAttribute(prefix, localName, namespaceUri);
            if (value is not null)
            {
                WriteStringValue(value);
            }

            WriteEndAttribute();
        }

        /// <summary>
        /// Writes a complete UTF-8 attribute.
        /// </summary>
        /// <param name="utf8Name">The UTF-8 name.</param>
        /// <param name="utf8Value">The UTF-8 value.</param>
        public void WriteAttributeString(ReadOnlySpan<byte> utf8Name, ReadOnlySpan<byte> utf8Value)
        {
            WriteStartAttributeUtf8(utf8Name, [], []);
            WriteStringValue(utf8Value);
            WriteEndAttribute();
        }

        /// <summary>
        /// Writes a complete attribute.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void WriteAttributeString(ReadOnlySpan<char> name, ReadOnlySpan<char> value)
        {
            WriteStartAttribute(name);
            WriteStringValue(value);
            WriteEndAttribute();
        }

        /// <summary>
        /// Begins an attribute.
        /// </summary>
        /// <param name="name">The attribute name.</param>
        public void WriteStartAttribute(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            WriteStartAttributeLiteral(name);
        }

        /// <summary>
        /// Begins an attribute.
        /// </summary>
        /// <param name="name">The attribute name.</param>
        public void WriteStartAttribute(ReadOnlySpan<char> name)
        {
            WriteStartAttributeChars(name, default, default);
        }

        /// <summary>
        /// Begins an attribute with an explicit prefix.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        public void WriteStartAttribute(string? prefix, string localName, string? namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(localName);
            WriteStartAttributeChars(localName, prefix.AsSpan(), namespaceUri.AsSpan());
        }

        /// <summary>
        /// Ends the current attribute.
        /// </summary>
        public void WriteEndAttribute()
        {
            ThrowIfDisposed();
            if (!_inAttribute)
            {
                throw new XmlException("No attribute to end.");
            }

            WriteRaw("\""u8);
            _inAttribute = false;
        }

        /// <summary>
        /// Writes an xmlns declaration for <paramref name="prefix"/>.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        public void WriteNamespaceDeclaration(string prefix, string namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            ArgumentNullException.ThrowIfNull(namespaceUri);
            WriteNamespaceDeclaration(
                EncodeUtf8(prefix, stackalloc byte[64], out _),
                EncodeUtf8(namespaceUri, stackalloc byte[256], out _));
        }

        /// <summary>
        /// Writes an xmlns declaration for UTF-8 <paramref name="utf8Prefix"/>.
        /// </summary>
        /// <param name="utf8Prefix">The UTF-8 prefix.</param>
        /// <param name="utf8NamespaceUri">The UTF-8 namespace URI.</param>
        public void WriteNamespaceDeclaration(ReadOnlySpan<byte> utf8Prefix, ReadOnlySpan<byte> utf8NamespaceUri)
        {
            RegisterNamespace(utf8Prefix, utf8NamespaceUri);
            WriteRegisteredNamespace(utf8Prefix, utf8NamespaceUri);
        }

        /// <summary>
        /// Validates a namespace declaration for the open start tag and puts it in scope without writing it.
        /// </summary>
        /// <param name="prefix">The prefix, or empty for the default namespace.</param>
        /// <param name="ns">The namespace URI. Empty undeclares.</param>
        /// <remarks>
        /// The DOM registers every declaration of a start tag first, then writes the tag's attributes in
        /// document order with <see cref="WriteRegisteredNamespace"/>. A prefixed attribute that precedes
        /// its own declaration (<c>&lt;e p:a="1" xmlns:p="u"/&gt;</c>) then finds the prefix bound instead
        /// of triggering a second, automatic declaration.
        /// </remarks>
        internal void RegisterNamespace(ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> ns)
        {
            ThrowIfDisposed();
            RequireOpenTag();
            if (prefix.IsEmpty)
            {
                if (!_options.SkipValidation && Utf8XmlReader.IsReservedNamespace(ns))
                {
                    throw new XmlException("The xml and xmlns namespaces cannot be declared as the default namespace.");
                }

                PushNs(_depth, [], ns);
                return;
            }

            RegisterPrefixedNamespace(prefix, ns);
        }

        /// <summary>
        /// Writes the text of a namespace declaration that <see cref="RegisterNamespace"/> already put in scope.
        /// </summary>
        /// <param name="prefix">The prefix, or empty for the default namespace.</param>
        /// <param name="ns">The namespace URI.</param>
        internal void WriteRegisteredNamespace(ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> ns)
        {
            if (prefix.IsEmpty)
            {
                TrackAttribute("xmlns"u8, XmlUtf8.Xmlns);
                WriteRaw(" xmlns=\""u8);
            }
            else
            {
                TrackAttribute(prefix, XmlUtf8.Xmlns);
                WriteRaw(" xmlns:"u8);
                WriteRaw(prefix);
                WriteRaw("=\""u8);
            }

            WriteAttributeText(ns);
            WriteRaw("\""u8);
        }

        /// <summary>
        /// Validates an <c>xmlns:prefix</c> declaration and puts it in scope.
        /// </summary>
        /// <param name="utf8Prefix">The prefix.</param>
        /// <param name="utf8NamespaceUri">The namespace URI.</param>
        internal void RegisterPrefixedNamespace(ReadOnlySpan<byte> utf8Prefix, ReadOnlySpan<byte> utf8NamespaceUri)
        {
            ValidateNcName(utf8Prefix);
            if (!_options.SkipValidation && utf8NamespaceUri.IsEmpty && _version is XmlVersion.Version10)
            {
                throw new XmlException("Prefix undeclaration is not allowed in XML 1.0.");
            }

            if (!_options.SkipValidation && utf8Prefix.SequenceEqual("xml"u8) && !utf8NamespaceUri.SequenceEqual(XmlUtf8.Xml))
            {
                throw new XmlException("The xml prefix cannot be rebound.");
            }

            if (!_options.SkipValidation && utf8Prefix.SequenceEqual("xmlns"u8))
            {
                throw new XmlException("The xmlns prefix cannot be declared.");
            }

            if (!_options.SkipValidation
                && !utf8Prefix.SequenceEqual("xml"u8)
                && Utf8XmlReader.IsReservedNamespace(utf8NamespaceUri))
            {
                throw new XmlException("The xml and xmlns namespaces cannot be bound to another prefix.");
            }

            PushNs(_depth, utf8Prefix, utf8NamespaceUri);
        }

        /// <summary>
        /// Writes a default xmlns declaration.
        /// </summary>
        /// <param name="namespaceUri">The namespace URI.</param>
        public void WriteDefaultNamespace(string namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(namespaceUri);
            WriteDefaultNamespace(EncodeUtf8(namespaceUri, stackalloc byte[256], out _));
        }

        /// <summary>
        /// Writes a default xmlns declaration.
        /// </summary>
        /// <param name="utf8NamespaceUri">The UTF-8 namespace URI.</param>
        public void WriteDefaultNamespace(ReadOnlySpan<byte> utf8NamespaceUri)
        {
            RegisterNamespace([], utf8NamespaceUri);
            WriteRegisteredNamespace([], utf8NamespaceUri);
        }

        /// <summary>
        /// Writes character data or attribute text.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteStringValue(string? value)
        {
            if (value is null)
            {
                return;
            }

            WriteStringValue(value.AsSpan());
        }

        /// <summary>
        /// Writes character data or attribute text.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteStringValue(ReadOnlySpan<char> value)
        {
            if (value.IndexOfAnyExcept(_inAttribute ? XmlChars.PlainAttributeChars : XmlChars.PlainTextChars) < 0)
            {
                if (_inAttribute)
                {
                    WriteCharsRaw(value);
                    return;
                }

                if (_depth == 0)
                {
                    throw new XmlException("Text is not allowed at the document level.");
                }

                CloseStartTag();
                MarkText();
                WriteCharsRaw(value);
                return;
            }

            var utf8 = EncodeUtf8(value, stackalloc byte[256], out var heap);
            WriteStringValue(utf8);
            _ = heap;
        }

        /// <summary>
        /// Writes UTF-8 character data or attribute text.
        /// </summary>
        /// <param name="utf8Value">The UTF-8 value.</param>
        public void WriteStringValue(ReadOnlySpan<byte> utf8Value)
        {
            ThrowIfDisposed();
            if (_inAttribute)
            {
                WriteAttributeText(utf8Value);
                return;
            }

            if (_depth == 0)
            {
                throw new XmlException("Text is not allowed at the document level.");
            }

            CloseStartTag();
            MarkText();
            WriteElementText(utf8Value);
        }

        /// <summary>
        /// Writes an XSD dateTime value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteStringValue(DateTime value)
        {
            var format = value.Kind switch
            {
                DateTimeKind.Utc => "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ",
                DateTimeKind.Local => "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz",
                _ => "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            };
            WriteFormatted(value, format);
        }

        /// <summary>
        /// Writes an XSD dateTime value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteStringValue(DateTimeOffset value)
        {
            WriteFormatted(value, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz");
        }

        /// <summary>
        /// Writes an XSD date value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteStringValue(DateOnly value)
        {
            WriteFormatted(value, "yyyy-MM-dd");
        }

        /// <summary>
        /// Writes an XSD time value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteStringValue(TimeOnly value)
        {
            WriteFormatted(value, "HH:mm:ss.FFFFFFF");
        }

        /// <summary>
        /// Writes an XSD duration value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteStringValue(TimeSpan value)
        {
            WriteStringValue(FormatDuration(value));
        }

        /// <summary>
        /// Writes a GUID value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteStringValue(Guid value)
        {
            WriteFormatted(value, "D");
        }

        /// <summary>
        /// Writes an integer value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteNumberValue(int value)
        {
            WriteNumberUtf8(value, 16);
        }

        /// <summary>
        /// Writes a 64-bit integer value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteNumberValue(long value)
        {
            WriteNumberUtf8(value, 24);
        }

        /// <summary>
        /// Writes a double value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteNumberValue(double value)
        {
            if (double.IsNaN(value))
            {
                WriteStringValue("NaN"u8);
                return;
            }

            if (double.IsPositiveInfinity(value))
            {
                WriteStringValue("INF"u8);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                WriteStringValue("-INF"u8);
                return;
            }

            WriteNumberUtf8(value, 32);
        }

        /// <summary>
        /// Writes a single value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteNumberValue(float value)
        {
            if (float.IsNaN(value))
            {
                WriteStringValue("NaN"u8);
                return;
            }

            if (float.IsPositiveInfinity(value))
            {
                WriteStringValue("INF"u8);
                return;
            }

            if (float.IsNegativeInfinity(value))
            {
                WriteStringValue("-INF"u8);
                return;
            }

            WriteNumberUtf8(value, 24);
        }

        /// <summary>
        /// Writes a decimal value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteNumberValue(decimal value)
        {
            WriteNumberUtf8(value, 32);
        }

        /// <summary>
        /// Writes an unsigned integer value.
        /// </summary>
        /// <param name="value">The value.</param>
        [CLSCompliant(false)]
        public void WriteNumberValue(uint value)
        {
            WriteNumberUtf8(value, 16);
        }

        /// <summary>
        /// Writes an unsigned 64-bit integer value.
        /// </summary>
        /// <param name="value">The value.</param>
        [CLSCompliant(false)]
        public void WriteNumberValue(ulong value)
        {
            WriteNumberUtf8(value, 24);
        }

        /// <summary>
        /// Writes <c>true</c> or <c>false</c>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteBooleanValue(bool value)
        {
            WriteStringValue(value ? "true"u8 : "false"u8);
        }

        /// <summary>
        /// Writes base64 text.
        /// </summary>
        /// <param name="bytes">The bytes to encode.</param>
        public void WriteBase64StringValue(ReadOnlySpan<byte> bytes)
        {
            ThrowIfDisposed();
            if (!_inAttribute)
            {
                if (_depth == 0)
                {
                    throw new XmlException("Text is not allowed at the document level.");
                }

                CloseStartTag();
                MarkText();
            }

            var max = Base64.GetMaxEncodedToUtf8Length(bytes.Length);
            Ensure(max);
            Base64.EncodeToUtf8(bytes, _memory.Span[_buffered..], out _, out var written);
            _buffered += written;
        }

        /// <summary>
        /// Writes a UTF-8 text segment, possibly incomplete.
        /// </summary>
        /// <param name="value">The UTF-8 segment.</param>
        /// <param name="isFinalSegment">True when this is the last segment of the value.</param>
        public void WriteStringValueSegment(ReadOnlySpan<byte> value, bool isFinalSegment)
        {
            _ = isFinalSegment;
            WriteStringValue(value);
        }

        /// <summary>
        /// Writes a text segment, possibly incomplete.
        /// </summary>
        /// <param name="value">The segment.</param>
        /// <param name="isFinalSegment">True when this is the last segment of the value.</param>
        public void WriteStringValueSegment(ReadOnlySpan<char> value, bool isFinalSegment)
        {
            _ = isFinalSegment;
            WriteStringValue(value);
        }

        /// <summary>
        /// Writes a base64 segment, possibly incomplete.
        /// </summary>
        /// <param name="value">The bytes.</param>
        /// <param name="isFinalSegment">True when this is the last segment.</param>
        public void WriteBase64StringSegment(ReadOnlySpan<byte> value, bool isFinalSegment)
        {
            _ = isFinalSegment;
            WriteBase64StringValue(value);
        }

        /// <summary>
        /// Writes a CDATA section.
        /// </summary>
        /// <param name="text">The CDATA payload.</param>
        public void WriteCData(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            WriteCData(text.AsSpan());
        }

        /// <summary>
        /// Writes a CDATA section.
        /// </summary>
        /// <param name="text">The CDATA payload.</param>
        public void WriteCData(ReadOnlySpan<char> text)
        {
            WriteCData(EncodeUtf8(text, stackalloc byte[256], out _));
        }

        /// <summary>
        /// Writes a CDATA section.
        /// </summary>
        /// <param name="utf8Value">The UTF-8 CDATA payload.</param>
        public void WriteCData(ReadOnlySpan<byte> utf8Value)
        {
            ThrowIfDisposed();
            if (_depth == 0)
            {
                throw new XmlException("CDATA is not allowed at the document level.");
            }

            if (utf8Value.IndexOf("]]>"u8) >= 0)
            {
                throw new XmlException("CDATA must not contain ']]>'.");
            }

            ValidateContentChars(utf8Value);

            CloseStartTag();
            MarkText();
            WriteRaw("<![CDATA["u8);
            WriteRaw(utf8Value);
            WriteRaw("]]>"u8);
        }

        /// <summary>
        /// Writes a comment.
        /// </summary>
        /// <param name="value">The comment payload.</param>
        public void WriteComment(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            WriteComment(value.AsSpan());
        }

        /// <summary>
        /// Writes a comment.
        /// </summary>
        /// <param name="value">The comment payload.</param>
        public void WriteComment(ReadOnlySpan<char> value)
        {
            WriteComment(EncodeUtf8(value, stackalloc byte[256], out _));
        }

        /// <summary>
        /// Writes a comment.
        /// </summary>
        /// <param name="utf8Value">The UTF-8 comment payload.</param>
        public void WriteComment(ReadOnlySpan<byte> utf8Value)
        {
            ThrowIfDisposed();
            if (utf8Value.IndexOf("--"u8) >= 0 || (utf8Value.Length > 0 && utf8Value[^1] is (byte)'-'))
            {
                throw new XmlException("Comments must not contain '--' or end with '-'.");
            }

            ValidateContentChars(utf8Value);

            if (_depth > 0)
            {
                CloseStartTag();
                MarkContent();
            }
            else
            {
                EnsureDeclaration();
            }

            WriteRaw("<!--"u8);
            WriteRaw(utf8Value);
            WriteRaw("-->"u8);
        }

        /// <summary>
        /// Writes a processing instruction.
        /// </summary>
        /// <param name="target">The PI target.</param>
        /// <param name="data">The PI data.</param>
        public void WriteProcessingInstruction(string target, string? data)
        {
            ArgumentNullException.ThrowIfNull(target);
            WriteProcessingInstructionUtf8(
                EncodeUtf8(target, stackalloc byte[64], out _),
                EncodeUtf8(data, stackalloc byte[256], out _));
        }

        /// <summary>
        /// Writes raw UTF-8 XML.
        /// </summary>
        /// <param name="utf8Xml">The raw bytes.</param>
        /// <param name="skipInputValidation">When true, the bytes are copied without checks.</param>
        public void WriteRawValue(ReadOnlySpan<byte> utf8Xml, bool skipInputValidation = false)
        {
            _ = skipInputValidation;
            ThrowIfDisposed();
            if (_depth > 0)
            {
                CloseStartTag();
                MarkText();
            }

            WriteRaw(utf8Xml);
        }

        /// <summary>
        /// Writes raw XML characters.
        /// </summary>
        /// <param name="xml">The raw XML.</param>
        /// <param name="skipInputValidation">When true, the text is copied without checks.</param>
        public void WriteRawValue(ReadOnlySpan<char> xml, bool skipInputValidation = false)
        {
            WriteRawValue(EncodeUtf8(xml, stackalloc byte[256], out _), skipInputValidation);
        }

        /// <summary>
        /// Writes raw XML.
        /// </summary>
        /// <param name="xml">The raw XML.</param>
        /// <param name="skipInputValidation">When true, the text is copied without checks.</param>
        public void WriteRawValue(string xml, bool skipInputValidation = false)
        {
            ArgumentNullException.ThrowIfNull(xml);
            WriteRawValue(xml.AsSpan(), skipInputValidation);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Closes an open start tag with <c>&gt;</c> when attributes are finished.
        /// </summary>
        internal void CloseStartTag()
        {
            if (_depth == 0)
            {
                return;
            }

            ref var frame = ref CurrentFrame();
            if (!frame.TagOpen)
            {
                return;
            }

            WriteRawByte((byte)'>');
            frame.TagOpen = false;
        }

        /// <summary>
        /// Returns a reference to the current element frame.
        /// </summary>
        /// <returns>The current frame.</returns>
        internal ref XmlWriterFrame CurrentFrame()
        {
            var index = _depth - 1;
            if (index < 8)
            {
                return ref _framesInline[index];
            }

            return ref _frames![index];
        }

        /// <summary>
        /// Encodes <paramref name="value"/> to UTF-8 on the heap.
        /// </summary>
        /// <param name="value">The characters.</param>
        /// <returns>UTF-8 bytes, or empty.</returns>
        internal static byte[] EncodeChars(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
            {
                return [];
            }

            var utf8 = new byte[Encoding.UTF8.GetByteCount(value)];
            Encoding.UTF8.GetBytes(value, utf8);
            return utf8;
        }

        /// <summary>
        /// Encodes <paramref name="value"/> into <paramref name="scratch"/> when it fits, otherwise onto the heap.
        /// </summary>
        /// <param name="value">The characters.</param>
        /// <param name="scratch">Stack scratch space.</param>
        /// <param name="heap">Heap bytes when scratch is too small, otherwise null.</param>
        /// <returns>The UTF-8 bytes.</returns>
        internal static ReadOnlySpan<byte> EncodeUtf8(ReadOnlySpan<char> value, Span<byte> scratch, out byte[]? heap)
        {
            heap = null;
            if (value.IsEmpty)
            {
                return [];
            }

            var max = Encoding.UTF8.GetMaxByteCount(value.Length);
            if (max <= scratch.Length)
            {
                var written = Encoding.UTF8.GetBytes(value, scratch);
                return scratch[..written];
            }

            heap = EncodeChars(value);
            return heap;
        }

        /// <summary>
        /// Writes a start tag from UTF-16 parts.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        internal void WriteStartElementChars(ReadOnlySpan<char> localName, ReadOnlySpan<char> prefix, ReadOnlySpan<char> namespaceUri)
        {
            WriteStartElementChars(localName, prefix, namespaceUri, empty: false);
        }

        /// <summary>
        /// Writes a start tag or empty element from UTF-16 parts.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <param name="empty">When true, closes as <c>/&gt;</c> without keeping a frame name.</param>
        internal void WriteStartElementChars(ReadOnlySpan<char> localName, ReadOnlySpan<char> prefix, ReadOnlySpan<char> namespaceUri, bool empty)
        {
            var localUtf8 = EncodeUtf8(localName, stackalloc byte[256], out var localHeap);
            var prefixUtf8 = EncodeUtf8(prefix, stackalloc byte[64], out var prefixHeap);
            var nsUtf8 = EncodeUtf8(namespaceUri, stackalloc byte[256], out var nsHeap);
            WriteStartElementUtf8(localUtf8, prefixUtf8, nsUtf8, empty);
            _ = localHeap;
            _ = prefixHeap;
            _ = nsHeap;
        }

        /// <summary>
        /// Writes an empty element from UTF-16 parts.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        internal void WriteEmptyElementChars(ReadOnlySpan<char> localName, ReadOnlySpan<char> prefix, ReadOnlySpan<char> namespaceUri)
        {
            WriteStartElementChars(localName, prefix, namespaceUri, empty: true);
        }

        /// <summary>
        /// Begins an attribute from UTF-16 parts.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        internal void WriteStartAttributeChars(ReadOnlySpan<char> localName, ReadOnlySpan<char> prefix, ReadOnlySpan<char> namespaceUri)
        {
            var localUtf8 = EncodeUtf8(localName, stackalloc byte[256], out var localHeap);
            var prefixUtf8 = EncodeUtf8(prefix, stackalloc byte[64], out var prefixHeap);
            var nsUtf8 = EncodeUtf8(namespaceUri, stackalloc byte[256], out var nsHeap);
            WriteStartAttributeUtf8(localUtf8, prefixUtf8, nsUtf8);
            _ = localHeap;
            _ = prefixHeap;
            _ = nsHeap;
        }

        /// <summary>
        /// Ensures <paramref name="needed"/> bytes of buffer remain.
        /// </summary>
        /// <param name="needed">The number of bytes about to be written.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Ensure(int needed)
        {
            if ((uint)_buffered + (uint)needed <= (uint)_memory.Length)
            {
                return;
            }

            Grow(needed);
        }

        /// <summary>
        /// Commits the current buffer and obtains a destination span of at least <paramref name="needed"/> bytes.
        /// </summary>
        /// <param name="needed">The number of bytes about to be written.</param>
        /// <remarks>
        /// <see cref="IBufferWriter{T}"/> destinations write into <c>GetMemory</c> like
        /// <c>Utf8JsonWriter</c>, so the catalog path does not rent a 16 KB staging buffer or copy
        /// the document on flush. Stream destinations keep a pooled buffer with BCL-style overflow
        /// growth.
        /// </remarks>
        internal void Grow(int needed)
        {
            if (_bufferWriter is not null)
            {
                if (_buffered > 0)
                {
                    _bufferWriter.Advance(_buffered);
                    _committed += _buffered;
                    _buffered = 0;
                }

                _memory = _bufferWriter.GetMemory(Math.Max(needed, 4096));
                if (_memory.Length == 0)
                {
                    throw new XmlException("The destination buffer is too small.");
                }

                return;
            }

            if (_buffered > 0)
            {
                _stream!.Write(_buffer, 0, _buffered);
                _committed += _buffered;
                _buffered = 0;
            }

            if (needed > _buffer.Length)
            {
                var rented = ArrayPool<byte>.Shared.Rent(needed);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = rented;
            }

            _memory = _buffer;
        }

        /// <summary>
        /// Returns the pending buffer to <see cref="ArrayPool{T}"/>.
        /// </summary>
        internal void ReturnBuffer()
        {
            if (_buffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = [];
            }

            _memory = default;
            _buffered = 0;
            if (_names.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(_names);
                _names = [];
                _namesLength = 0;
            }
        }

        /// <summary>
        /// Appends <paramref name="utf8"/> to the name store.
        /// </summary>
        /// <param name="utf8">The UTF-8 name.</param>
        /// <returns>The stored slice.</returns>
        internal XmlWriterName AppendName(ReadOnlySpan<byte> utf8)
        {
            if (utf8.IsEmpty)
            {
                return default;
            }

            EnsureNameStore(utf8.Length);
            utf8.CopyTo(_names.AsSpan(_namesLength));
            var name = XmlWriterName.From(_namesLength, utf8.Length);
            _namesLength += utf8.Length;
            return name;
        }

        /// <summary>
        /// Appends an ASCII or UTF-16 name to the name store without a UTF-8 transcode when the name is ASCII.
        /// </summary>
        /// <param name="chars">The name characters.</param>
        /// <returns>The stored slice.</returns>
        internal XmlWriterName AppendNameFromChars(ReadOnlySpan<char> chars)
        {
            if (chars.IsEmpty)
            {
                return default;
            }

            if (Ascii.IsValid(chars))
            {
                EnsureNameStore(chars.Length);
                Encoding.Latin1.GetBytes(chars, _names.AsSpan(_namesLength));
                var name = XmlWriterName.From(_namesLength, chars.Length);
                _namesLength += chars.Length;
                return name;
            }

            var utf8 = EncodeUtf8(chars, stackalloc byte[256], out var heap);
            var stored = AppendName(utf8);
            _ = heap;
            return stored;
        }

        /// <summary>
        /// Returns the UTF-8 bytes for <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The stored slice.</param>
        /// <returns>The UTF-8 span.</returns>
        internal ReadOnlySpan<byte> NameSpan(XmlWriterName name)
        {
            return _names.AsSpan(name.Start, name.Length);
        }

        /// <summary>
        /// Writes a UTF-8 BOM when <see cref="XmlWriterOptions.EmitByteOrderMark"/> is set and nothing has been written.
        /// </summary>
        internal void EnsureBom()
        {
            if (_emittedBom || !_options.EmitByteOrderMark)
            {
                return;
            }

            WriteRaw([(byte)0xEF, (byte)0xBB, (byte)0xBF]);
            _emittedBom = true;
        }

        /// <summary>
        /// Writes the XML declaration when it has not been omitted and has not been written yet.
        /// </summary>
        internal void EnsureDeclaration()
        {
            if (!_wroteDeclaration && !_options.OmitXmlDeclaration)
            {
                EmitDeclaration();
                return;
            }

            EnsureBom();
        }

        /// <summary>
        /// Writes the XML declaration (and BOM when requested).
        /// </summary>
        internal void EmitDeclaration()
        {
            EnsureBom();
            WriteRaw("<?xml version=\""u8);
            WriteRaw(_version is XmlVersion.Version11 ? "1.1"u8 : "1.0"u8);
            WriteRaw("\" encoding=\"utf-8\""u8);
            if (_options.Standalone is { } standalone)
            {
                WriteRaw(standalone ? " standalone=\"yes\""u8 : " standalone=\"no\""u8);
            }

            WriteRaw("?>"u8);
            _wroteDeclaration = true;
        }

        /// <summary>
        /// Ensures the XML Schema instance namespace is in scope as prefix <c>xsi</c>.
        /// </summary>
        internal void EnsureXsi()
        {
            if (LookupPrefix(XmlUtf8.Xsi) is not null)
            {
                return;
            }

            WriteNamespaceDeclaration("xsi"u8, XmlUtf8.Xsi);
        }

        /// <summary>
        /// Writes buffered bytes to the destination.
        /// </summary>
        internal void FlushBuffer()
        {
            if (_buffered == 0)
            {
                return;
            }

            if (_stream is not null)
            {
                _stream.Write(_buffer, 0, _buffered);
                _committed += _buffered;
                _buffered = 0;
                return;
            }

            _bufferWriter!.Advance(_buffered);
            _committed += _buffered;
            _buffered = 0;
            _memory = default;
        }

        /// <summary>
        /// Formats <paramref name="value"/> as XSD duration.
        /// </summary>
        /// <param name="value">The duration.</param>
        /// <returns>The lexical form.</returns>
        internal static string FormatDuration(TimeSpan value)
        {
            if (value == TimeSpan.Zero)
            {
                return "PT0S";
            }

            var negative = value < TimeSpan.Zero;
            if (negative)
            {
                value = value.Negate();
            }

            var text = new StringBuilder(negative ? "-P" : "P");
            if (value.Days > 0)
            {
                text.Append(value.Days).Append('D');
            }

            if (value.Hours > 0 || value.Minutes > 0 || value.Seconds > 0 || value.Milliseconds > 0)
            {
                text.Append('T');
                if (value.Hours > 0)
                {
                    text.Append(value.Hours).Append('H');
                }

                if (value.Minutes > 0)
                {
                    text.Append(value.Minutes).Append('M');
                }

                if (value.Seconds > 0 || value.Milliseconds > 0)
                {
                    if (value.Milliseconds > 0)
                    {
                        text.Append(CultureInfo.InvariantCulture, $"{value.Seconds}.{value.Milliseconds:D3}").Append('S');
                    }
                    else
                    {
                        text.Append(value.Seconds).Append('S');
                    }
                }
            }

            return text.ToString();
        }

        /// <summary>
        /// Returns the in-scope prefix bound to <paramref name="uri"/>, or null.
        /// </summary>
        /// <param name="uri">The namespace URI.</param>
        /// <returns>The prefix bytes, empty for the default xmlns, or null if unbound.</returns>
        internal byte[]? LookupPrefix(ReadOnlySpan<byte> uri)
        {
            for (var i = _nsCount - 1; i >= 0; i--)
            {
                var binding = NsAt(i);
                if (uri.SequenceEqual(binding.Uri))
                {
                    return binding.Prefix;
                }
            }

            if (uri.SequenceEqual(XmlUtf8.Xml))
            {
                return XmlUtf8.XmlPrefix;
            }

            if (uri.SequenceEqual(XmlUtf8.Xmlns))
            {
                return XmlUtf8.XmlnsPrefix;
            }

            return null;
        }

        /// <summary>
        /// Applies <paramref name="options"/> to the writer.
        /// </summary>
        /// <param name="options">The options.</param>
        internal void Configure(in XmlWriterOptions options)
        {
            _options = options;
            _indentSize = XmlLimits.Resolve(options.IndentSize, 2);
            _maxAttributes = XmlLimits.Resolve(options.MaxAttributeCount, XmlLimits.DefaultMaxAttributeCount);
            _maxDepth = XmlLimits.Resolve(options.MaxDepth, XmlLimits.DefaultMaxDepth);
            _maxNameBytes = XmlLimits.Resolve(options.MaxNameByteCount, XmlLimits.DefaultMaxNameByteCount);
            _newLineUtf8 = null;
            _version = options.Version;
        }

        /// <summary>
        /// Drops everything not yet flushed, forgets the destination, returns rented arrays, and leaves the
        /// writer ready for <see cref="Reset(IBufferWriter{byte})"/> or <see cref="Reset(Stream)"/>.
        /// </summary>
        /// <remarks>
        /// The serializer calls this when a write fails part way through a document, so the half-written
        /// bytes still in the buffer never reach the destination, and when it puts a writer back in its cache,
        /// so the cache keeps neither the caller's destination nor a pooled array alive.
        /// </remarks>
        internal void Detach()
        {
            _buffered = 0;
            Reset();
            _bufferWriter = null;
            _stream = null;
            ReturnBuffer();
        }

        /// <summary>
        /// Marks the current element as having content.
        /// </summary>
        internal void MarkContent()
        {
            if (_depth > 0)
            {
                CurrentFrame().HasContent = true;
            }
        }

        /// <summary>
        /// Checks the characters of a comment, CDATA section, or processing instruction payload.
        /// </summary>
        /// <param name="utf8">The payload.</param>
        /// <exception cref="XmlException">
        /// The payload is not valid UTF-8, holds a character outside the Char production of the output version,
        /// or holds an XML 1.1 RestrictedChar.
        /// </exception>
        /// <remarks>
        /// Character references are not recognized in these constructs, so a character that cannot appear
        /// literally cannot be written at all. Element text and attribute values escape it instead.
        /// </remarks>
        internal void ValidateContentChars(ReadOnlySpan<byte> utf8)
        {
            if (_options.SkipValidation || utf8.IndexOfAnyExcept(XmlChars.AsciiContentChars) < 0)
            {
                return;
            }

            var i = 0;
            while (i < utf8.Length)
            {
                if (!TryReadRune(utf8, ref i, out var cp))
                {
                    throw new XmlException("Invalid UTF-8 in XML content.");
                }

                var legal = _version is XmlVersion.Version11
                    ? XmlChars.IsXml11Char(cp) && !XmlChars.IsXml11RestrictedChar(cp)
                    : XmlChars.IsXml10Char(cp);
                if (!legal)
                {
                    throw new XmlException("Illegal character in a comment, CDATA section, or processing instruction.");
                }
            }
        }

        /// <summary>
        /// Rejects the <c>xmlns</c> prefix on an element name.
        /// </summary>
        /// <param name="prefix">The UTF-8 element prefix.</param>
        /// <exception cref="XmlException">The prefix is <c>xmlns</c> (Namespaces in XML, section 3: element names must not use it).</exception>
        internal void ValidateElementPrefix(ReadOnlySpan<byte> prefix)
        {
            if (!_options.SkipValidation && prefix.SequenceEqual(XmlUtf8.XmlnsPrefix))
            {
                throw new XmlException("Element names must not use the xmlns prefix.");
            }
        }

        /// <summary>
        /// Rejects the <c>xmlns</c> prefix on an element name.
        /// </summary>
        /// <param name="prefix">The element prefix.</param>
        /// <exception cref="XmlException">The prefix is <c>xmlns</c> (Namespaces in XML, section 3: element names must not use it).</exception>
        internal void ValidateElementPrefix(ReadOnlySpan<char> prefix)
        {
            if (!_options.SkipValidation && prefix.SequenceEqual("xmlns"))
            {
                throw new XmlException("Element names must not use the xmlns prefix.");
            }
        }

        /// <summary>
        /// Marks the current element as having character data.
        /// </summary>
        /// <remarks>
        /// Whitespace is significant next to character data, so an element that holds text is never
        /// indented: not its child start tags, and not its end tag.
        /// </remarks>
        internal void MarkText()
        {
            if (_depth > 0)
            {
                ref var frame = ref CurrentFrame();
                frame.HasContent = true;
                frame.HasText = true;
            }
        }

        /// <summary>
        /// Formats <paramref name="value"/> as UTF-8 on the stack with the invariant culture and writes it as a value.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="format">The format string. Its longest output must fit in 64 bytes.</param>
        internal void WriteFormatted<T>(T value, ReadOnlySpan<char> format)
            where T : struct, IUtf8SpanFormattable
        {
            Span<byte> utf8 = stackalloc byte[64];
            value.TryFormat(utf8, out var written, format, CultureInfo.InvariantCulture);
            WriteStringValue(utf8[..written]);
        }

        /// <summary>
        /// Writes the newline and indent before a child start tag, unless the parent holds character data.
        /// </summary>
        /// <param name="depth">The depth of the element about to be written.</param>
        internal void WriteElementIndent(int depth)
        {
            if (_options.Indented && (_depth == 0 || !CurrentFrame().HasText))
            {
                WriteIndent(depth);
            }
        }

        /// <summary>
        /// Records that an XML declaration was already written (for example by <see cref="XmlDocument.WriteTo"/>).
        /// </summary>
        /// <remarks>
        /// Prevents <see cref="EnsureDeclaration"/> from emitting a second declaration when
        /// <see cref="XmlWriterOptions.OmitXmlDeclaration"/> is false.
        /// </remarks>
        internal void MarkDeclarationWritten()
        {
            _wroteDeclaration = true;
        }

        /// <summary>
        /// Writes an xmlns declaration for <paramref name="prefix"/>, or a default xmlns declaration when it is empty.
        /// </summary>
        /// <param name="prefix">The prefix, or empty for the default namespace.</param>
        /// <param name="ns">The namespace URI. Empty undeclares.</param>
        internal void DeclareNamespace(ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> ns)
        {
            RegisterNamespace(prefix, ns);
            WriteRegisteredNamespace(prefix, ns);
        }

        /// <summary>
        /// Declares the namespace of the open start tag's own name unless the in-scope binding already matches.
        /// </summary>
        /// <param name="prefix">The element prefix, or empty.</param>
        /// <param name="ns">The element namespace URI, or empty for no namespace.</param>
        /// <remarks>
        /// Pairs with <see cref="WriteStartElementDeferred"/>. An unprefixed element in no namespace under a
        /// non-empty default namespace gets <c>xmlns=""</c>, which is the only way to keep it out of that
        /// namespace (Namespaces in XML, section 6.2).
        /// </remarks>
        internal void EnsureElementNamespace(ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> ns)
        {
            if (!prefix.IsEmpty && ns.IsEmpty)
            {
                if (!_options.SkipValidation)
                {
                    throw new XmlException("Prefixed names require a namespace URI.");
                }

                return;
            }

            if (ns.SequenceEqual(LookupUri(prefix)))
            {
                return;
            }

            DeclareNamespace(prefix, ns);
        }

        /// <summary>
        /// Writes <c>&lt;prefix:local</c> and opens the element without declaring any namespace.
        /// </summary>
        /// <param name="local">The local name.</param>
        /// <param name="prefix">The prefix, or empty.</param>
        /// <remarks>
        /// The DOM replays an element's own xmlns declarations in document order and then calls
        /// <see cref="EnsureElementNamespace"/>. Declaring up front, as <see cref="WriteStartElementUtf8(ReadOnlySpan{byte}, ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        /// does, would drop declarations that are only referenced from content (<c>xsi:type="p:T"</c>).
        /// </remarks>
        internal void WriteStartElementDeferred(ReadOnlySpan<byte> local, ReadOnlySpan<byte> prefix)
        {
            ThrowIfDisposed();
            ValidateNcName(local);
            if (!prefix.IsEmpty)
            {
                ValidateNcName(prefix);
                ValidateElementPrefix(prefix);
            }

            BeginStartElement();
            WriteTagOpen(prefix, local);
            PushFrame(AppendName(local), AppendName(prefix));
            _depth++;
            _seenRoot = true;
        }

        /// <summary>
        /// Writes a complete element with a UTF-8 name and text content.
        /// </summary>
        /// <param name="local">The local name.</param>
        /// <param name="prefix">The prefix, or empty.</param>
        /// <param name="ns">The namespace URI, or empty.</param>
        /// <param name="utf8Text">The unescaped text content.</param>
        /// <remarks>
        /// When the text needs no escaping and the name needs no new namespace declaration, the whole
        /// <c>&lt;n&gt;v&lt;/n&gt;</c> run is written in one reserved span and no frame is pushed.
        /// Anything else takes the start tag, text, end tag route and produces the same bytes.
        /// </remarks>
        internal void WriteCompleteElementUtf8(ReadOnlySpan<byte> local, ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> ns, ReadOnlySpan<byte> utf8Text)
        {
            ThrowIfDisposed();
            var nameIsBound = _nsCount == 0 && prefix.IsEmpty
                ? ns.IsEmpty
                : ns.SequenceEqual(LookupUri(prefix));
            if (!nameIsBound || _options.Indented || utf8Text.IndexOfAnyExcept(XmlChars.AsciiTextWriteChars) >= 0)
            {
                WriteStartElementDeferred(local, prefix);
                EnsureElementNamespace(prefix, ns);
                WriteStringValue(utf8Text);
                WriteFullEndElement();
                return;
            }

            ValidateNcName(local);
            var nameLength = local.Length;
            if (!prefix.IsEmpty)
            {
                ValidateNcName(prefix);
                ValidateElementPrefix(prefix);
                nameLength += prefix.Length + 1;
            }

            BeginStartElement();
            Ensure(5 + (2 * nameLength) + utf8Text.Length);
            var dest = _memory.Span[_buffered..];
            dest[0] = (byte)'<';
            var pos = 1;
            if (!prefix.IsEmpty)
            {
                prefix.CopyTo(dest[pos..]);
                pos += prefix.Length;
                dest[pos++] = (byte)':';
            }

            local.CopyTo(dest[pos..]);
            pos += local.Length;
            dest[pos++] = (byte)'>';
            utf8Text.CopyTo(dest[pos..]);
            pos += utf8Text.Length;
            dest[pos++] = (byte)'<';
            dest[pos++] = (byte)'/';
            dest.Slice(1, nameLength).CopyTo(dest[pos..]);
            pos += nameLength;
            dest[pos++] = (byte)'>';
            _buffered += pos;
            _seenRoot = true;
        }

        /// <summary>
        /// Writes a processing instruction from UTF-8 parts.
        /// </summary>
        /// <param name="utf8Target">The UTF-8 PI target.</param>
        /// <param name="utf8Data">The UTF-8 PI data, or empty.</param>
        /// <remarks>
        /// <see cref="XmlDocument.WriteTo"/> replays parsed processing instructions through this overload
        /// without materializing strings.
        /// </remarks>
        internal void WriteProcessingInstructionUtf8(ReadOnlySpan<byte> utf8Target, ReadOnlySpan<byte> utf8Data)
        {
            ThrowIfDisposed();
            ValidateNcName(utf8Target);
            if (!_options.SkipValidation && utf8Target.Length == 3 && (utf8Target[0] | 0x20) is (byte)'x' && (utf8Target[1] | 0x20) is (byte)'m' && (utf8Target[2] | 0x20) is (byte)'l')
            {
                throw new XmlException("Processing instruction target cannot be 'xml'.");
            }

            if (utf8Data.IndexOf("?>"u8) >= 0)
            {
                throw new XmlException("Processing instruction data must not contain '?>'.");
            }

            ValidateContentChars(utf8Data);

            if (_depth > 0)
            {
                CloseStartTag();
                MarkContent();
            }
            else
            {
                EnsureDeclaration();
            }

            WriteRaw("<?"u8);
            WriteRaw(utf8Target);
            if (!utf8Data.IsEmpty)
            {
                WriteRaw(" "u8);
                WriteRaw(utf8Data);
            }

            WriteRaw("?>"u8);
        }

        /// <summary>
        /// Returns the namespace binding at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The binding index.</param>
        /// <returns>The binding.</returns>
        internal XmlWriterNsBinding NsAt(int index)
        {
            return _nsHeap![index];
        }

        /// <summary>
        /// Pops xmlns bindings owned by <paramref name="depth"/>.
        /// </summary>
        /// <param name="depth">The ending element depth.</param>
        internal void PopNs(int depth)
        {
            while (_nsCount > 0 && NsAt(_nsCount - 1).Depth == depth)
            {
                _nsCount--;
                _nsCacheMask = 0;
            }
        }

        /// <summary>
        /// Pushes an xmlns binding.
        /// </summary>
        /// <param name="depth">The owning depth.</param>
        /// <param name="prefix">The prefix bytes.</param>
        /// <param name="uri">The URI bytes.</param>
        /// <remarks>
        /// A popped slot keeps its arrays. Documents declare the same namespaces over and over (sibling
        /// elements, or the next document after <see cref="Reset()"/>), so a push whose bytes match the
        /// slot's previous occupant reuses those arrays instead of allocating new ones.
        /// </remarks>
        internal void PushNs(int depth, ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> uri)
        {
            if (_nsHeap is null)
            {
                _nsHeap = new XmlWriterNsBinding[8];
            }
            else if (_nsCount == _nsHeap.Length)
            {
                Array.Resize(ref _nsHeap, _nsHeap.Length * 2);
            }

            var stale = _nsHeap[_nsCount];
            var ownedPrefix = stale.Prefix is { } stalePrefix && prefix.SequenceEqual(stalePrefix) ? stalePrefix : InternNamespaceBytes(prefix);
            var ownedUri = stale.Uri is { } staleUri && uri.SequenceEqual(staleUri) ? staleUri : InternNamespaceBytes(uri);
            _nsHeap[_nsCount] = new XmlWriterNsBinding(depth, ownedPrefix, ownedUri);
            _nsCount++;
            _nsCacheMask = 0;
        }

        /// <summary>
        /// Throws if attributes are not allowed.
        /// </summary>
        internal void RequireOpenTag()
        {
            if (_depth == 0 || !CurrentFrame().TagOpen || _inAttribute)
            {
                throw new XmlException("Attributes must be written on an open start tag.");
            }
        }

        /// <summary>
        /// Throws if the writer has been disposed.
        /// </summary>
        internal void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        /// <summary>
        /// Validates <paramref name="name"/> as an XML Name.
        /// </summary>
        /// <param name="name">The UTF-8 name.</param>
        internal void ValidateName(ReadOnlySpan<byte> name)
        {
            if (_options.SkipValidation)
            {
                return;
            }

            if (name.IsEmpty)
            {
                throw new XmlException("Name is empty.");
            }

            if (name.Length > _maxNameBytes)
            {
                throw new XmlException($"{nameof(XmlWriterOptions.MaxNameByteCount)} exceeded ({_maxNameBytes}).");
            }

            if (XmlChars.IsAsciiNameStart(name[0]) && name[0] is not (byte)':' && name.IndexOfAnyExcept(XmlChars.AsciiNcNameChars) < 0)
            {
                return;
            }

            var i = 0;
            if (!TryReadRune(name, ref i, out var cp) || !XmlChars.IsNameStartChar(cp, _version))
            {
                throw new XmlException("Invalid XML name.");
            }

            while (i < name.Length)
            {
                if (!TryReadRune(name, ref i, out cp) || !XmlChars.IsNameChar(cp, _version))
                {
                    throw new XmlException("Invalid XML name.");
                }
            }
        }

        /// <summary>
        /// Validates <paramref name="name"/> as an NCName (a Name with no colon).
        /// </summary>
        /// <param name="name">The UTF-8 name.</param>
        internal void ValidateNcName(ReadOnlySpan<byte> name)
        {
            if (XmlChars.IsShortAsciiNcName(name, Math.Min(_maxNameBytes, 16)))
            {
                return;
            }

            ValidateName(name);
            if (!_options.SkipValidation && name.IndexOf((byte)':') >= 0)
            {
                throw new XmlException("Invalid XML name.");
            }
        }

        /// <summary>
        /// Validates a UTF-16 NCName. ASCII names use the same start/NcName checks as BCL <c>CheckNCName</c>.
        /// </summary>
        /// <param name="name">The name characters.</param>
        internal void ValidateNcNameChars(ReadOnlySpan<char> name)
        {
            if (_options.SkipValidation)
            {
                return;
            }

            if (name.IsEmpty)
            {
                throw new XmlException("Name is empty.");
            }

            if (Ascii.IsValid(name))
            {
                if (name.Length > _maxNameBytes)
                {
                    throw new XmlException($"{nameof(XmlWriterOptions.MaxNameByteCount)} exceeded ({_maxNameBytes}).");
                }

                var start = (byte)name[0];
                if (!XmlChars.IsAsciiNameStart(start) || start is (byte)':')
                {
                    throw new XmlException("Invalid XML name.");
                }

                for (var i = 1; i < name.Length; i++)
                {
                    if (!XmlChars.IsAsciiNcNameChar((byte)name[i]))
                    {
                        throw new XmlException("Invalid XML name.");
                    }
                }

                return;
            }

            var utf8 = EncodeUtf8(name, stackalloc byte[256], out var heap);
            ValidateNcName(utf8);
            _ = heap;
        }

        /// <summary>
        /// Increments the current start tag's attribute count, including xmlns*.
        /// </summary>
        internal void IncrementAttributeCount()
        {
            ref var frame = ref CurrentFrame();
            if (frame.AttributeCount >= _maxAttributes)
            {
                throw new XmlException($"{nameof(XmlWriterOptions.MaxAttributeCount)} exceeded ({_maxAttributes}).");
            }

            frame.AttributeCount++;
        }

        /// <summary>
        /// Records an attribute key and throws when <c>{local, ns}</c> is already on the start tag.
        /// </summary>
        /// <param name="local">The local name.</param>
        /// <param name="ns">The namespace URI.</param>
        internal void RememberAttribute(ReadOnlySpan<byte> local, ReadOnlySpan<byte> ns)
        {
            ref var frame = ref CurrentFrame();
            var count = frame.AttributeCount;
            for (var i = 0; i < count; i++)
            {
                if (NameSpan(AttrLocalAt(i)).SequenceEqual(local) && NameSpan(AttrNsAt(i)).SequenceEqual(ns))
                {
                    throw new XmlException("Duplicate attribute.");
                }
            }
        }

        /// <summary>
        /// Stores the attribute key at the current count, then increments.
        /// </summary>
        /// <param name="local">The local name.</param>
        /// <param name="ns">The namespace URI.</param>
        internal void TrackAttribute(ReadOnlySpan<byte> local, ReadOnlySpan<byte> ns)
        {
            if (!_options.SkipValidation)
            {
                RememberAttribute(local, ns);
            }

            StoreAttribute(AppendName(local), AppendName(ns));
        }

        /// <summary>
        /// Records an attribute from UTF-16 names without a UTF-8 transcode when the name is ASCII.
        /// </summary>
        /// <param name="local">The local name.</param>
        /// <param name="ns">The namespace URI.</param>
        internal void TrackAttributeChars(ReadOnlySpan<char> local, ReadOnlySpan<char> ns)
        {
            var storedLocal = AppendNameFromChars(local);
            var storedNs = AppendNameFromChars(ns);
            if (!_options.SkipValidation)
            {
                RememberAttribute(NameSpan(storedLocal), NameSpan(storedNs));
            }

            StoreAttribute(storedLocal, storedNs);
        }

        /// <summary>
        /// Stores an attribute key at the current count and increments.
        /// </summary>
        /// <param name="storedLocal">The stored local name.</param>
        /// <param name="storedNs">The stored namespace URI.</param>
        internal void StoreAttribute(XmlWriterName storedLocal, XmlWriterName storedNs)
        {
            var index = CurrentFrame().AttributeCount;
            if (index < 8)
            {
                _attrLocalsInline[index] = storedLocal;
                _attrNsInline[index] = storedNs;
            }
            else
            {
                if (_attrLocalsHeap is null)
                {
                    _attrLocalsHeap = new XmlWriterName[16];
                    _attrNsHeap = new XmlWriterName[16];
                    for (var i = 0; i < 8; i++)
                    {
                        _attrLocalsHeap[i] = _attrLocalsInline[i];
                        _attrNsHeap[i] = _attrNsInline[i];
                    }
                }
                else if (index == _attrLocalsHeap.Length)
                {
                    Array.Resize(ref _attrLocalsHeap, _attrLocalsHeap.Length * 2);
                    Array.Resize(ref _attrNsHeap, _attrNsHeap!.Length * 2);
                }

                _attrLocalsHeap[index] = storedLocal;
                _attrNsHeap![index] = storedNs;
            }

            IncrementAttributeCount();
        }

        /// <summary>
        /// Returns the recorded attribute local name at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The attribute index.</param>
        /// <returns>The local name.</returns>
        internal XmlWriterName AttrLocalAt(int index)
        {
            return index < 8 ? _attrLocalsInline[index] : _attrLocalsHeap![index];
        }

        /// <summary>
        /// Returns the recorded attribute namespace at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The attribute index.</param>
        /// <returns>The namespace URI.</returns>
        internal XmlWriterName AttrNsAt(int index)
        {
            return index < 8 ? _attrNsInline[index] : _attrNsHeap![index];
        }

        /// <summary>
        /// Decodes one UTF-8 rune from <paramref name="span"/> at <paramref name="index"/>.
        /// </summary>
        /// <param name="span">The UTF-8 bytes.</param>
        /// <param name="index">The byte index, advanced on success.</param>
        /// <param name="cp">The code point.</param>
        /// <returns><see langword="true"/> if a rune was decoded.</returns>
        internal static bool TryReadRune(ReadOnlySpan<byte> span, ref int index, out int cp)
        {
            cp = 0;
            if (index >= span.Length)
            {
                return false;
            }

            var b = span[index];
            if (b < 0x80)
            {
                cp = b;
                index++;
                return true;
            }

            var status = Rune.DecodeFromUtf8(span[index..], out var rune, out var consumed);
            if (status is not OperationStatus.Done)
            {
                return false;
            }

            cp = rune.Value;
            index += consumed;
            return true;
        }

        /// <summary>
        /// Writes UTF-8 bytes to the buffer.
        /// </summary>
        /// <param name="utf8">The bytes.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void WriteRaw(ReadOnlySpan<byte> utf8)
        {
            if (utf8.IsEmpty)
            {
                return;
            }

            Ensure(utf8.Length);
            utf8.CopyTo(_memory.Span[_buffered..]);
            _buffered += utf8.Length;
        }

        /// <summary>
        /// Writes one UTF-8 byte, matching BCL <c>_bufBytes[_bufPos++] = (byte)ch</c> for markup.
        /// </summary>
        /// <param name="value">The byte.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void WriteRawByte(byte value)
        {
            if (_buffered == _memory.Length)
            {
                Ensure(1);
            }

            _memory.Span[_buffered] = value;
            _buffered++;
        }

        /// <summary>
        /// Writes UTF-16 as UTF-8. ASCII is copied as bytes, matching BCL <c>XmlUtf8RawTextWriter.RawText</c>.
        /// </summary>
        /// <param name="chars">The characters.</param>
        internal void WriteCharsRaw(ReadOnlySpan<char> chars)
        {
            if (chars.IsEmpty)
            {
                return;
            }

            Ensure(chars.Length);
            var status = Ascii.FromUtf16(chars, _memory.Span[_buffered..], out var written);
            if (status is OperationStatus.Done)
            {
                _buffered += written;
                return;
            }

            if (written > 0)
            {
                _buffered += written;
                chars = chars[written..];
            }

            var utf8 = EncodeUtf8(chars, stackalloc byte[256], out var heap);
            WriteRaw(utf8);
            _ = heap;
        }

        /// <summary>
        /// Writes escaped element text.
        /// </summary>
        /// <param name="utf8">The raw UTF-8 text.</param>
        internal void WriteElementText(ReadOnlySpan<byte> utf8)
        {
            if (utf8.IndexOfAnyExcept(XmlChars.AsciiTextWriteChars) < 0)
            {
                WriteRaw(utf8);
                return;
            }

            var bracket = 0;
            var i = 0;
            while (i < utf8.Length)
            {
                if (bracket >= 2 && utf8[i] is (byte)'>')
                {
                    // "]]>" must not appear in character data (XML 1.0 and 1.1 section 2.4).
                    WriteRaw("&gt;"u8);
                    i++;
                    bracket = 0;
                    continue;
                }

                var plain = utf8[i..].IndexOfAnyExcept(XmlChars.AsciiTextWriteChars);
                if (plain != 0)
                {
                    plain = plain < 0 ? utf8.Length - i : plain;
                    WriteRaw(utf8.Slice(i, plain));
                    i += plain;
                    bracket = 0;
                    continue;
                }

                int cp = utf8[i];
                if (cp >= 0x80)
                {
                    i += WriteMultiByteRun(utf8[i..]);
                    bracket = 0;
                    continue;
                }

                var start = i;
                i++;
                if (cp is (byte)'&')
                {
                    WriteRaw("&amp;"u8);
                    bracket = 0;
                    continue;
                }

                if (cp is '\r')
                {
                    // A literal CR would be read back as LF (XML 1.0 and 1.1 section 2.11).
                    WriteRaw("&#xD;"u8);
                    bracket = 0;
                    continue;
                }

                if (cp is '<')
                {
                    WriteRaw("&lt;"u8);
                    bracket = 0;
                    continue;
                }

                WriteValueSlice(utf8[start..i], cp);
                bracket = cp is ']' ? bracket + 1 : 0;
            }
        }

        /// <summary>
        /// Writes escaped attribute text.
        /// </summary>
        /// <param name="utf8">The raw UTF-8 text.</param>
        internal void WriteAttributeText(ReadOnlySpan<byte> utf8)
        {
            if (utf8.IndexOfAnyExcept(XmlChars.AsciiAttributeWriteChars) < 0)
            {
                WriteRaw(utf8);
                return;
            }

            var i = 0;
            while (i < utf8.Length)
            {
                var plain = utf8[i..].IndexOfAnyExcept(XmlChars.AsciiAttributeWriteChars);
                if (plain != 0)
                {
                    plain = plain < 0 ? utf8.Length - i : plain;
                    WriteRaw(utf8.Slice(i, plain));
                    i += plain;
                    continue;
                }

                int cp = utf8[i];
                if (cp >= 0x80)
                {
                    i += WriteMultiByteRun(utf8[i..]);
                    continue;
                }

                var start = i;
                i++;
                if (cp is (byte)'&')
                {
                    WriteRaw("&amp;"u8);
                    continue;
                }

                if (cp is '\t' or '\n' or '\r')
                {
                    // Literal TAB, LF, and CR in an attribute value are read back as a space
                    // (attribute-value normalization, XML 1.0 and 1.1 section 3.3.3).
                    WriteNcr(cp);
                    continue;
                }

                if (cp is '<')
                {
                    WriteRaw("&lt;"u8);
                    continue;
                }

                if (cp is '"')
                {
                    WriteRaw("&quot;"u8);
                    continue;
                }

                WriteValueSlice(utf8[start..i], cp);
            }
        }

        /// <summary>
        /// Writes <paramref name="slice"/> or a numeric character reference when required.
        /// </summary>
        /// <param name="slice">The UTF-8 encoding of <paramref name="cp"/>.</param>
        /// <param name="cp">The Unicode code point.</param>
        internal void WriteValueSlice(ReadOnlySpan<byte> slice, int cp)
        {
            if (cp is 0)
            {
                throw new XmlException("Illegal character in XML value.");
            }

            if (_version is XmlVersion.Version11)
            {
                if (!XmlChars.IsXml11Char(cp))
                {
                    throw new XmlException("Illegal character in XML value.");
                }

                if (XmlChars.IsXml11RestrictedChar(cp) || cp is 0x85 or 0x2028)
                {
                    // RestrictedChar must be a reference. NEL and LS are legal literals but would be
                    // read back as LF (XML 1.1 section 2.11), so they are written as references too.
                    WriteNcr(cp);
                    return;
                }
            }
            else if (!XmlChars.IsXml10Char(cp))
            {
                throw new XmlException("Illegal character in XML value.");
            }

            WriteRaw(slice);
        }

        /// <summary>
        /// Writes the run of multi-byte characters at the start of <paramref name="utf8"/>.
        /// </summary>
        /// <param name="utf8">Text whose first byte starts a multi-byte UTF-8 sequence.</param>
        /// <returns>The number of bytes consumed: everything up to the next ASCII byte.</returns>
        /// <exception cref="XmlException">The run is not valid UTF-8, or holds a character XML does not allow.</exception>
        /// <remarks>
        /// A valid run is copied in one piece when none of its characters needs a reference. Only XML 1.1
        /// has such characters outside ASCII (the C1 controls, NEL, and LS), and they all start with 0xC2 or 0xE2.
        /// </remarks>
        internal int WriteMultiByteRun(ReadOnlySpan<byte> utf8)
        {
            var ascii = utf8.IndexOfAnyInRange((byte)0x00, (byte)0x7F);
            var run = ascii < 0 ? utf8 : utf8[..ascii];
            if (Utf8.IsValid(run)
                && !XmlChars.ContainsNonCharacter(run)
                && (_version is XmlVersion.Version10 || run.IndexOfAny((byte)0xC2, (byte)0xE2) < 0))
            {
                WriteRaw(run);
                return run.Length;
            }

            var i = 0;
            while (i < run.Length)
            {
                var start = i;
                if (!TryReadRune(run, ref i, out var cp))
                {
                    throw new XmlException("Invalid UTF-8 in XML value.");
                }

                WriteValueSlice(run[start..i], cp);
            }

            return run.Length;
        }

        /// <summary>
        /// Writes a hexadecimal numeric character reference for <paramref name="cp"/>.
        /// </summary>
        /// <param name="cp">The code point.</param>
        internal void WriteNcr(int cp)
        {
            Span<byte> hex = stackalloc byte[8];
            Utf8Formatter.TryFormat(cp, hex, out var written, 'X');
            WriteRaw("&#x"u8);
            WriteRaw(hex[..written]);
            WriteRaw(";"u8);
        }

        /// <summary>
        /// Writes <paramref name="b"/> or a numeric character reference when required.
        /// </summary>
        /// <param name="b">The byte.</param>
        internal void WriteInvalidOrByte(byte b)
        {
            WriteValueSlice([b], b);
        }

        /// <summary>
        /// Writes indent for <paramref name="depth"/>.
        /// </summary>
        /// <param name="depth">The indent depth.</param>
        internal void WriteIndent(int depth)
        {
            if (!_options.Indented)
            {
                return;
            }

            if (depth <= 0 && _buffered == 0 && _committed == 0)
            {
                return;
            }

            WriteRaw(NewLineUtf8());
            var ch = _options.IndentCharacter == '\0' ? (byte)' ' : (byte)_options.IndentCharacter;
            var count = Math.Max(depth, 0) * _indentSize;
            Ensure(count);
            _memory.Span.Slice(_buffered, count).Fill(ch);
            _buffered += count;
        }

        /// <summary>
        /// Writes a start tag from UTF-8 parts.
        /// </summary>
        /// <param name="local">The local name.</param>
        /// <param name="prefix">The prefix, or empty.</param>
        /// <param name="ns">The namespace URI, or empty.</param>
        internal void WriteStartElementUtf8(ReadOnlySpan<byte> local, ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> ns)
        {
            WriteStartElementUtf8(local, prefix, ns, empty: false);
        }

        /// <summary>
        /// Closes a parent start tag, writes indent, and enforces depth before a new element.
        /// </summary>
        internal void BeginStartElement()
        {
            ThrowIfDisposed();
            if (_inAttribute)
            {
                WriteEndAttribute();
            }

            if (_depth == 0 && _seenRoot)
            {
                throw new XmlException("Multiple root elements are not allowed.");
            }

            if (_depth > 0)
            {
                CloseStartTag();
                MarkContent();
            }
            else
            {
                EnsureDeclaration();
            }

            if (_depth + 1 > _maxDepth)
            {
                throw new XmlException($"{nameof(XmlWriterOptions.MaxDepth)} exceeded ({_maxDepth}).");
            }

            WriteElementIndent(_depth);
        }

        /// <summary>
        /// Writes a start tag from UTF-16 names using BCL-style ASCII raw encoding.
        /// </summary>
        /// <param name="local">The local name.</param>
        /// <param name="prefix">The prefix, or empty.</param>
        /// <param name="ns">The UTF-8 namespace URI, or empty.</param>
        /// <param name="empty">When true, writes <c>/&gt;</c> and does not keep the name on the stack.</param>
        internal void WriteStartElementFromChars(ReadOnlySpan<char> local, ReadOnlySpan<char> prefix, ReadOnlySpan<byte> ns, bool empty)
        {
            ValidateElementPrefix(prefix);
            var nsUri = ns;
            if (!prefix.IsEmpty && nsUri.IsEmpty)
            {
                if (prefix.Equals("xml", StringComparison.Ordinal))
                {
                    nsUri = XmlUtf8.Xml;
                }
                else if (prefix.Equals("xmlns", StringComparison.Ordinal))
                {
                    nsUri = XmlUtf8.Xmlns;
                }
                else if (!_options.SkipValidation)
                {
                    throw new XmlException("Prefixed names require a namespace URI.");
                }
            }

            BeginStartElement();
            WriteRaw("<"u8);
            byte[]? autoPrefix = null;
            if (!prefix.IsEmpty)
            {
                WriteCharsRaw(prefix);
                WriteRaw(":"u8);
            }
            else if (!nsUri.IsEmpty)
            {
                var bound = LookupPrefix(nsUri);
                if (bound is { Length: > 0 })
                {
                    WriteRaw(bound);
                    WriteRaw(":"u8);
                    autoPrefix = bound;
                }
            }

            WriteCharsRaw(local);
            var declarePrefixed = !prefix.IsEmpty && !nsUri.IsEmpty && LookupPrefix(nsUri) is null;
            var declareDefault = prefix.IsEmpty && !nsUri.IsEmpty && LookupPrefix(nsUri) is null;
            if (empty)
            {
                if (!declarePrefixed && !declareDefault)
                {
                    WriteRaw("/>"u8);
                    _seenRoot = true;
                    return;
                }

                PushFrame(default, default(XmlWriterName));
                _depth++;
                _seenRoot = true;
                if (declarePrefixed)
                {
                    WriteNamespaceDeclaration(EncodeUtf8(prefix, stackalloc byte[64], out _), nsUri);
                }
                else
                {
                    WriteDefaultNamespace(nsUri);
                }

                WriteEndElement();
                return;
            }

            PushFrame(AppendNameFromChars(local), autoPrefix is null ? AppendNameFromChars(prefix) : AppendName(autoPrefix));
            _depth++;
            _seenRoot = true;
            if (declarePrefixed)
            {
                WriteNamespaceDeclaration(EncodeUtf8(prefix, stackalloc byte[64], out _), nsUri);
            }
            else if (declareDefault)
            {
                WriteDefaultNamespace(nsUri);
            }
        }

        /// <summary>
        /// Writes a start tag from UTF-8 parts, optionally closing it as empty.
        /// </summary>
        /// <param name="local">The local name.</param>
        /// <param name="prefix">The prefix, or empty.</param>
        /// <param name="ns">The namespace URI, or empty.</param>
        /// <param name="empty">When true, writes <c>/&gt;</c> and does not keep the name on the stack.</param>
        internal void WriteStartElementUtf8(ReadOnlySpan<byte> local, ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> ns, bool empty)
        {
            ThrowIfDisposed();
            ValidateNcName(local);
            if (!prefix.IsEmpty)
            {
                ValidateNcName(prefix);
                ValidateElementPrefix(prefix);
            }

            var nsUri = ns;
            if (!prefix.IsEmpty && nsUri.IsEmpty)
            {
                if (prefix.SequenceEqual("xml"u8))
                {
                    nsUri = XmlUtf8.Xml;
                }
                else if (prefix.SequenceEqual("xmlns"u8))
                {
                    nsUri = XmlUtf8.Xmlns;
                }
                else if (!_options.SkipValidation)
                {
                    throw new XmlException("Prefixed names require a namespace URI.");
                }
            }

            if (_inAttribute)
            {
                WriteEndAttribute();
            }

            if (_depth == 0 && _seenRoot)
            {
                throw new XmlException("Multiple root elements are not allowed.");
            }

            if (_depth > 0)
            {
                CloseStartTag();
                MarkContent();
            }
            else
            {
                EnsureDeclaration();
            }

            if (_depth + 1 > _maxDepth)
            {
                throw new XmlException($"{nameof(XmlWriterOptions.MaxDepth)} exceeded ({_maxDepth}).");
            }

            WriteElementIndent(_depth);
            byte[]? autoPrefix = null;
            if (prefix.IsEmpty && !nsUri.IsEmpty && LookupPrefix(nsUri) is { Length: > 0 } bound)
            {
                autoPrefix = bound;
            }

            WriteTagOpen(autoPrefix is null ? prefix : autoPrefix, local);
            var declarePrefixed = !prefix.IsEmpty && !nsUri.IsEmpty && LookupPrefix(nsUri) is null;
            var declareDefault = prefix.IsEmpty && !nsUri.IsEmpty && LookupPrefix(nsUri) is null;
            if (empty)
            {
                if (!declarePrefixed && !declareDefault)
                {
                    WriteRaw("/>"u8);
                    _seenRoot = true;
                    return;
                }

                PushFrame(default, default(XmlWriterName));
                _depth++;
                _seenRoot = true;
                if (declarePrefixed)
                {
                    WriteNamespaceDeclaration(prefix, nsUri);
                }
                else
                {
                    WriteDefaultNamespace(nsUri);
                }

                WriteEndElement();
                return;
            }

            PushFrame(AppendName(local), AppendName(autoPrefix ?? prefix));
            _depth++;
            _seenRoot = true;
            if (declarePrefixed)
            {
                WriteNamespaceDeclaration(prefix, nsUri);
            }
            else if (declareDefault)
            {
                WriteDefaultNamespace(nsUri);
            }
        }

        /// <summary>
        /// Begins an attribute from UTF-8 parts.
        /// </summary>
        /// <param name="local">The local name.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="ns">The namespace URI.</param>
        internal void WriteStartAttributeUtf8(ReadOnlySpan<byte> local, ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> ns)
        {
            ThrowIfDisposed();
            RequireOpenTag();
            ValidateNcName(local);
            if (!prefix.IsEmpty)
            {
                ValidateNcName(prefix);
            }

            var nsUri = ns;
            if (!prefix.IsEmpty && nsUri.IsEmpty)
            {
                if (prefix.SequenceEqual("xml"u8))
                {
                    nsUri = XmlUtf8.Xml;
                }
                else if (prefix.SequenceEqual("xmlns"u8))
                {
                    nsUri = XmlUtf8.Xmlns;
                }
                else if (!_options.SkipValidation)
                {
                    throw new XmlException("Prefixed names require a namespace URI.");
                }
            }

            byte[]? bound = null;
            if (!prefix.IsEmpty && !nsUri.IsEmpty)
            {
                var current = LookupUri(prefix);
                if (current is null || !nsUri.SequenceEqual(current))
                {
                    WriteNamespaceDeclaration(prefix, nsUri);
                }
            }
            else if (prefix.IsEmpty && !nsUri.IsEmpty)
            {
                bound = LookupPrefixedPrefix(nsUri);
                if (bound is null)
                {
                    var generated = NextAutoPrefix();
                    WriteNamespaceDeclaration(generated, nsUri);
                    bound = generated;
                }
            }

            TrackAttribute(local, nsUri);
            WriteRaw(" "u8);
            if (!prefix.IsEmpty)
            {
                WriteRaw(prefix);
                WriteRaw(":"u8);
            }
            else if (bound is { Length: > 0 })
            {
                WriteRaw(bound);
                WriteRaw(":"u8);
            }

            WriteRaw(local);
            WriteRaw("=\""u8);
            _inAttribute = true;
        }

        /// <summary>
        /// Returns an in-scope non-empty prefix bound to <paramref name="uri"/>, or null.
        /// </summary>
        /// <param name="uri">The namespace URI.</param>
        /// <returns>The prefix bytes, or null.</returns>
        internal byte[]? LookupPrefixedPrefix(ReadOnlySpan<byte> uri)
        {
            for (var i = _nsCount - 1; i >= 0; i--)
            {
                var binding = NsAt(i);
                if (binding.Prefix.Length > 0 && uri.SequenceEqual(binding.Uri))
                {
                    return binding.Prefix;
                }
            }

            if (uri.SequenceEqual(XmlUtf8.Xml))
            {
                return XmlUtf8.XmlPrefix;
            }

            if (uri.SequenceEqual(XmlUtf8.Xmlns))
            {
                return XmlUtf8.XmlnsPrefix;
            }

            return null;
        }

        /// <summary>
        /// Returns true when <paramref name="prefix"/> is already in scope.
        /// </summary>
        /// <param name="prefix">The prefix bytes.</param>
        /// <returns><see langword="true"/> if the prefix is bound.</returns>
        internal bool PrefixInUse(ReadOnlySpan<byte> prefix)
        {
            return LookupUri(prefix) is not null;
        }

        /// <summary>
        /// Returns the URI bound to <paramref name="prefix"/>, or null.
        /// </summary>
        /// <param name="prefix">The prefix bytes.</param>
        /// <returns>The URI bytes, or null if the prefix is unbound.</returns>
        /// <remarks>
        /// A document uses a handful of prefixes over and over, usually declared on its root. A small
        /// direct-mapped cache from prefix to binding index, emptied whenever a binding is pushed or
        /// popped, answers those with one compare instead of a scan of every binding in scope.
        /// </remarks>
        internal byte[]? LookupUri(ReadOnlySpan<byte> prefix)
        {
            var slot = prefix.IsEmpty ? 0 : (prefix[0] ^ (prefix[^1] << 1) ^ prefix.Length) & 7;
            if ((_nsCacheMask & (1 << slot)) != 0)
            {
                var cached = NsAt(_nsCacheIndexes[slot]);
                if (prefix.SequenceEqual(cached.Prefix))
                {
                    return cached.Uri;
                }
            }

            for (var i = _nsCount - 1; i >= 0; i--)
            {
                var binding = NsAt(i);
                if (prefix.SequenceEqual(binding.Prefix))
                {
                    _nsCacheMask |= 1 << slot;
                    _nsCacheIndexes[slot] = i;
                    return binding.Uri;
                }
            }

            if (prefix.SequenceEqual("xml"u8))
            {
                return XmlUtf8.Xml;
            }

            if (prefix.SequenceEqual("xmlns"u8))
            {
                return XmlUtf8.Xmlns;
            }

            return null;
        }

        /// <summary>
        /// Returns an array this writer owns that holds <paramref name="value"/>, reusing one from an earlier declaration.
        /// </summary>
        /// <param name="value">A namespace prefix or URI.</param>
        /// <returns>The owned bytes.</returns>
        /// <remarks>
        /// A binding slot remembers the last declaration it held, which covers a document whose declarations
        /// repeat in the same order. Sibling elements that declare different prefixes land in the same slot
        /// in turn, so the writer also keeps the first <see cref="MaxInternedNamespaceValues"/> distinct
        /// values it has seen. Past that limit a value is copied each time it is declared.
        /// </remarks>
        internal byte[] InternNamespaceBytes(ReadOnlySpan<byte> value)
        {
            for (var i = 0; i < _nsInternedCount; i++)
            {
                if (value.SequenceEqual(_nsInterned![i]))
                {
                    return _nsInterned[i];
                }
            }

            var owned = value.ToArray();
            if (_nsInternedCount < MaxInternedNamespaceValues)
            {
                _nsInterned ??= new byte[MaxInternedNamespaceValues][];
                _nsInterned[_nsInternedCount] = owned;
                _nsInternedCount++;
            }

            return owned;
        }

        /// <summary>
        /// Returns the next auto-generated prefix <c>nsN</c> that is not in scope.
        /// </summary>
        /// <returns>The prefix bytes.</returns>
        internal byte[] NextAutoPrefix()
        {
            Span<byte> buffer = stackalloc byte[16];
            "ns"u8.CopyTo(buffer);
            while (true)
            {
                _autoNs++;
                Utf8Formatter.TryFormat(_autoNs, buffer[2..], out var written);
                var prefix = buffer[..(2 + written)];
                if (!PrefixInUse(prefix))
                {
                    return prefix.ToArray();
                }
            }
        }

        /// <summary>
        /// Returns the slot the next element's frame goes in, growing the stack when it is full.
        /// </summary>
        /// <returns>The slot at the current depth. It still holds whatever frame used it last.</returns>
        internal ref XmlWriterFrame NextFrame()
        {
            if (_depth < 8)
            {
                return ref _framesInline[_depth];
            }

            if (_frames is null)
            {
                _frames = new XmlWriterFrame[16];
                for (var i = 0; i < 8; i++)
                {
                    _frames[i] = _framesInline[i];
                }
            }
            else if (_depth == _frames.Length)
            {
                Array.Resize(ref _frames, _frames.Length * 2);
            }

            return ref _frames[_depth];
        }

        /// <summary>
        /// Pushes the frame of an element whose name is in the name buffer.
        /// </summary>
        /// <param name="local">Where the local name is in the name buffer.</param>
        /// <param name="prefix">Where the prefix is in the name buffer; empty for none.</param>
        /// <remarks>
        /// The frame is built in its slot. Passing a whole frame and storing it copies a struct that holds
        /// references, which goes through the collector's block move on every start tag.
        /// </remarks>
        internal void PushFrame(XmlWriterName local, XmlWriterName prefix)
        {
            ref var frame = ref NextFrame();
            frame = default;
            frame.Local = local;
            frame.Prefix = prefix;
            frame.TagOpen = true;
        }

        /// <summary>
        /// Pushes the frame of an element whose name is held as strings.
        /// </summary>
        /// <param name="localChars">The local name.</param>
        /// <param name="prefixChars">The prefix, or <see langword="null"/> for none.</param>
        internal void PushFrame(string localChars, string? prefixChars)
        {
            ref var frame = ref NextFrame();
            frame = default;
            frame.LocalChars = localChars;
            frame.PrefixChars = prefixChars;
            frame.TagOpen = true;
        }

        /// <summary>
        /// Returns the UTF-8 newline for indented output. <c>\n</c> and <c>\r\n</c> are static.
        /// </summary>
        /// <returns>The newline bytes.</returns>
        internal ReadOnlySpan<byte> NewLineUtf8()
        {
            if (_newLineUtf8 is not null)
            {
                return _newLineUtf8;
            }

            var nl = string.IsNullOrEmpty(_options.NewLine) ? Environment.NewLine : _options.NewLine;
            if (nl.Length == 1 && nl[0] == '\n')
            {
                return "\n"u8;
            }

            if (nl.Length == 2 && nl[0] == '\r' && nl[1] == '\n')
            {
                return "\r\n"u8;
            }

            _newLineUtf8 = Encoding.UTF8.GetBytes(nl);
            return _newLineUtf8;
        }

        /// <summary>
        /// Grows the name store so <paramref name="needed"/> more bytes fit.
        /// </summary>
        /// <param name="needed">Additional bytes to store.</param>
        internal void EnsureNameStore(int needed)
        {
            if (_names.Length == 0)
            {
                _names = ArrayPool<byte>.Shared.Rent(Math.Max(256, needed));
                return;
            }

            if (_namesLength + needed <= _names.Length)
            {
                return;
            }

            var rented = ArrayPool<byte>.Shared.Rent(Math.Max(_names.Length * 2, _namesLength + needed));
            _names.AsSpan(0, _namesLength).CopyTo(rented);
            ArrayPool<byte>.Shared.Return(_names);
            _names = rented;
        }

        /// <summary>
        /// Writes the prefix and local name stored on <paramref name="frame"/>.
        /// </summary>
        /// <param name="frame">The element frame.</param>
        /// <remarks>
        /// String start tags keep interned literals on the frame (BCL <c>ElementScope</c>) so the
        /// end tag does not copy UTF-8 out of the name store.
        /// </remarks>
        internal void WriteFrameName(in XmlWriterFrame frame)
        {
            if (frame.PrefixChars is { Length: > 0 } prefixChars)
            {
                WriteCharsRaw(prefixChars);
                WriteRawByte((byte)':');
            }
            else if (frame.Prefix.Length > 0)
            {
                WriteRaw(NameSpan(frame.Prefix));
                WriteRawByte((byte)':');
            }

            if (frame.LocalChars is not null)
            {
                WriteCharsRaw(frame.LocalChars);
                return;
            }

            WriteRaw(NameSpan(frame.Local));
        }

        /// <summary>
        /// Writes the end tag of <paramref name="frame"/>.
        /// </summary>
        /// <param name="frame">The element's frame.</param>
        /// <remarks>
        /// A name held in the name buffer is written with <c>&lt;/</c> and <c>&gt;</c> in one step, because
        /// each separate write looks up the output span again.
        /// </remarks>
        internal void WriteTagClose(in XmlWriterFrame frame)
        {
            if (frame.LocalChars is not null || frame.PrefixChars is not null)
            {
                WriteRaw("</"u8);
                WriteFrameName(frame);
                WriteRawByte((byte)'>');
                return;
            }

            var prefix = NameSpan(frame.Prefix);
            var local = NameSpan(frame.Local);
            var prefixed = prefix.Length > 0 ? prefix.Length + 1 : 0;
            Ensure(prefixed + local.Length + 3);
            var destination = _memory.Span[_buffered..];
            destination[0] = (byte)'<';
            destination[1] = (byte)'/';
            if (prefixed > 0)
            {
                prefix.CopyTo(destination[2..]);
                destination[prefixed + 1] = (byte)':';
            }

            local.CopyTo(destination[(prefixed + 2)..]);
            destination[prefixed + local.Length + 2] = (byte)'>';
            _buffered += prefixed + local.Length + 3;
        }

        /// <summary>
        /// Writes <c>&lt;</c>, the prefix and its colon when there is one, and the local name, in one step.
        /// </summary>
        /// <param name="prefix">The prefix, or empty.</param>
        /// <param name="local">The local name.</param>
        internal void WriteTagOpen(ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> local)
        {
            var prefixed = prefix.Length > 0 ? prefix.Length + 1 : 0;
            Ensure(prefixed + local.Length + 1);
            var destination = _memory.Span[_buffered..];
            destination[0] = (byte)'<';
            if (prefixed > 0)
            {
                prefix.CopyTo(destination[1..]);
                destination[prefixed] = (byte)':';
            }

            local.CopyTo(destination[(prefixed + 1)..]);
            _buffered += prefixed + local.Length + 1;
        }

        /// <summary>
        /// Writes a start tag from string names, keeping interned literals on the frame when there is no xmlns work.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="prefix">The prefix, or null.</param>
        /// <param name="namespaceUri">The namespace URI, or null.</param>
        /// <param name="empty">When true, writes <c>/&gt;</c> without pushing a frame.</param>
        internal void WriteStartElementLiteral(string localName, string? prefix, string? namespaceUri, bool empty)
        {
            if (!string.IsNullOrEmpty(namespaceUri))
            {
                WriteStartElementChars(localName, prefix.AsSpan(), namespaceUri.AsSpan(), empty);
                return;
            }

            ValidateNcNameChars(localName);
            if (!string.IsNullOrEmpty(prefix))
            {
                ValidateNcNameChars(prefix);
                ValidateElementPrefix(prefix);
                if (prefix is not "xml" and not "xmlns" && !_options.SkipValidation)
                {
                    throw new XmlException("Prefixed names require a namespace URI.");
                }
            }

            BeginStartElement();
            WriteRawByte((byte)'<');
            if (!string.IsNullOrEmpty(prefix))
            {
                WriteCharsRaw(prefix);
                WriteRawByte((byte)':');
            }

            WriteCharsRaw(localName);
            if (empty)
            {
                WriteRaw("/>"u8);
                _seenRoot = true;
                return;
            }

            PushFrame(localName, prefix);
            _depth++;
            _seenRoot = true;
        }

        /// <summary>
        /// Writes a complete unprefixed element without pushing a frame.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The text, ignored when <paramref name="valueIsNull"/> is true.</param>
        /// <param name="valueIsNull">When true, writes an empty element.</param>
        internal void WriteCompleteElement(string localName, string? value, bool valueIsNull)
        {
            ValidateNcNameChars(localName);
            BeginStartElement();
            WriteRawByte((byte)'<');
            WriteCharsRaw(localName);
            if (valueIsNull)
            {
                WriteRaw("/>"u8);
                _seenRoot = true;
                return;
            }

            WriteRawByte((byte)'>');
            WriteElementContentChars(value.AsSpan());
            WriteRaw("</"u8);
            WriteCharsRaw(localName);
            WriteRawByte((byte)'>');
            _seenRoot = true;
        }

        /// <summary>
        /// Writes a complete unprefixed element whose content is already UTF-8.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="utf8Value">The UTF-8 content, already escaped as character data.</param>
        internal void WriteCompleteElementUtf8Value(string localName, ReadOnlySpan<byte> utf8Value)
        {
            ValidateNcNameChars(localName);
            BeginStartElement();
            if (!_options.Indented)
            {
                var name = localName.AsSpan();
                Ensure(5 + (2 * name.Length) + utf8Value.Length);
                var dest = _memory.Span[_buffered..];
                dest[0] = (byte)'<';
                if (Ascii.FromUtf16(name, dest[1..], out var n) is OperationStatus.Done)
                {
                    dest[1 + n] = (byte)'>';
                    utf8Value.CopyTo(dest[(2 + n)..]);
                    var pos = 2 + n + utf8Value.Length;
                    dest[pos++] = (byte)'<';
                    dest[pos++] = (byte)'/';
                    Ascii.FromUtf16(name, dest[pos..], out _);
                    pos += n;
                    dest[pos++] = (byte)'>';
                    _buffered += pos;
                    _seenRoot = true;
                    return;
                }
            }

            WriteRawByte((byte)'<');
            WriteCharsRaw(localName);
            WriteRawByte((byte)'>');
            WriteRaw(utf8Value);
            WriteRaw("</"u8);
            WriteCharsRaw(localName);
            WriteRawByte((byte)'>');
            _seenRoot = true;
        }

        /// <summary>
        /// Writes element character data from UTF-16 without closing a start tag.
        /// </summary>
        /// <param name="value">The characters.</param>
        internal void WriteElementContentChars(ReadOnlySpan<char> value)
        {
            if (value.IndexOfAnyExcept(XmlChars.PlainTextChars) < 0)
            {
                WriteCharsRaw(value);
                return;
            }

            var utf8 = EncodeUtf8(value, stackalloc byte[256], out var heap);
            WriteElementText(utf8);
            _ = heap;
        }

        /// <summary>
        /// Formats <paramref name="value"/> into the output without going through the text escaper.
        /// </summary>
        /// <typeparam name="T">A UTF-8 span-formattable number.</typeparam>
        /// <param name="value">The number.</param>
        /// <param name="maxBytes">Maximum UTF-8 digits to reserve.</param>
        internal void WriteNumberUtf8<T>(T value, int maxBytes)
            where T : struct, IUtf8SpanFormattable
        {
            ThrowIfDisposed();
            if (_inAttribute)
            {
                Ensure(maxBytes);
            }
            else
            {
                if (_depth == 0)
                {
                    throw new XmlException("Text is not allowed at the document level.");
                }

                CloseStartTag();
                MarkText();
                Ensure(maxBytes);
            }

            value.TryFormat(_memory.Span[_buffered..], out var written, default, CultureInfo.InvariantCulture);
            _buffered += written;
        }

        /// <summary>
        /// Begins an unprefixed attribute from a string name without a UTF-8 transcode when the name is ASCII.
        /// </summary>
        /// <param name="name">The attribute name.</param>
        internal void WriteStartAttributeLiteral(string name)
        {
            RequireOpenTag();
            ValidateNcNameChars(name);
            TrackAttributeChars(name, []);
            Ensure(1 + name.Length + 2);
            WriteRawByte((byte)' ');
            WriteCharsRaw(name);
            WriteRaw("=\""u8);
            _inAttribute = true;
        }

        /// <summary>
        /// Writes a complete unprefixed integer attribute without entering attribute state.
        /// </summary>
        /// <param name="name">The attribute name.</param>
        /// <param name="value">The value.</param>
        internal void WriteAttributeNumberLiteral(string name, int value)
        {
            RequireOpenTag();
            ValidateNcNameChars(name);
            TrackAttributeChars(name, []);
            Ensure(1 + name.Length + 2 + 16 + 1);
            var dest = _memory.Span[_buffered..];
            dest[0] = (byte)' ';
            if (Ascii.FromUtf16(name, dest[1..], out var n) is not OperationStatus.Done)
            {
                WriteRawByte((byte)' ');
                WriteCharsRaw(name);
                WriteRaw("=\""u8);
                Ensure(16);
                value.TryFormat(_memory.Span[_buffered..], out var fallbackDigits, default, CultureInfo.InvariantCulture);
                _buffered += fallbackDigits;
                WriteRawByte((byte)'"');
                return;
            }

            var pos = 1 + n;
            dest[pos++] = (byte)'=';
            dest[pos++] = (byte)'"';
            value.TryFormat(dest[pos..], out var digits, default, CultureInfo.InvariantCulture);
            pos += digits;
            dest[pos++] = (byte)'"';
            _buffered += pos;
        }

        #endregion

    }

}
