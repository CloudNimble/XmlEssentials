using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// A <see langword="ref struct"/> UTF-8 XML tokenizer analogous to <c>Utf8JsonReader</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// var xml = "&lt;root id=\"1\"&gt;hi&lt;/root&gt;"u8;
    /// var reader = new Utf8XmlReader(xml);
    /// while (reader.Read())
    /// {
    ///     if (reader.TokenType == XmlTokenType.Text)
    ///     {
    ///         Console.WriteLine(reader.GetString());
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Empty elements are reported as <see cref="XmlTokenType.StartElement"/> with
    /// <see cref="IsEmptyElement"/> true. The reader does not synthesize an
    /// <see cref="XmlTokenType.EndElement"/> token. Callers must check
    /// <see cref="IsEmptyElement"/> before expecting a matching end tag.
    /// xmlns attributes bind before the element name is resolved. Unprefixed attributes
    /// are in no namespace; unprefixed elements use the default xmlns.
    /// The XML declaration, when present, must be the first token in the document (after an optional BOM).
    /// </remarks>
    public ref partial struct Utf8XmlReader
    {

        #region Fields

        /// <summary>
        /// <c>_nsDefaultIndex</c> value meaning the default-namespace binding has not been looked up since the bindings last changed.
        /// </summary>
        internal const int NsIndexUnknown = -2;

        /// <summary>
        /// Binding start that names no bytes.
        /// </summary>
        internal const int OwnedEmpty = -1;

        /// <summary>
        /// Binding start that names the first interned array; each later array is one lower.
        /// </summary>
        internal const int OwnedFirstInterned = -5;

        /// <summary>
        /// Binding start that names the <c>xml</c> namespace.
        /// </summary>
        internal const int OwnedXml = -2;

        /// <summary>
        /// Binding start that names the <c>xmlns</c> namespace.
        /// </summary>
        internal const int OwnedXmlns = -3;

        /// <summary>
        /// Binding start that names the XML Schema instance namespace.
        /// </summary>
        internal const int OwnedXsi = -4;

        /// <summary>
        /// Slot flag: a child element has started inside this element.
        /// </summary>
        internal const int ElementHasChild = 1 << 29;

        /// <summary>
        /// Slot flag: character data has been reported inside this element, so later whitespace is significant.
        /// </summary>
        internal const int ElementHasText = 1 << 28;

        /// <summary>
        /// Mask for the prefix length stored in the low bits of an element-stack slot.
        /// </summary>
        internal const int ElementPrefixLengthMask = (1 << 28) - 1;

        /// <summary>
        /// Slot flag: <c>xml:space="preserve"</c> is in force for this element.
        /// </summary>
        internal const int ElementPreserveSpace = 1 << 30;

        private int _attributeCount;
        private int _attributeIndex;
        private byte[]? _attributeDecoded;
        private int _attributeDecodedLength;
        private AttributeRecord[]? _attributes;
        private AttributeRecord8 _attributesInline;
        private int _currentNsLength;
        private byte[] _currentNsOwned;
        private int _currentNsStart;
        private int _decodedLength;
        private byte[]? _decodedValue;
        private int _depth;
        private long _documentBytesConsumedBase;
        private int _elementCount;
        private int[]? _elementNsLengths;
        private Int8Buffer _elementNsLengthsInline;
        private int[]? _elementNsStarts;
        private Int8Buffer _elementNsStartsInline;
        private bool _elementNsStored;
        private byte[][]? _elementNsUris;
        private int[]? _elementNameLengths;
        private Int8Buffer _elementNameLengthsInline;
        private int[]? _elementNameStarts;
        private Int8Buffer _elementNameStartsInline;
        private int[]? _elementPrefixLengths;
        private Int8Buffer _elementPrefixLengthsInline;
        private bool _finished;
        private bool _hasNamespaceAttribute;
        private bool _hasUnresolvedAttribute;
        private bool _hasXmlSpaceAttribute;
        private readonly bool _hasSequence;
        private bool _inAttribute;
        private bool _incomplete;
        private int _internedUriCount;
        private byte[][]? _internedUris;
        private bool _isEmptyElement;
        private readonly bool _isFinalBlock;
        private long _lineNumber;
        private long _lineStart;
        private readonly int _maxAttributes;
        private readonly int _maxCommentBytes;
        private readonly int _maxDepth;
        private readonly int _maxNameBytes;
        private readonly int _maxTextBytes;
        private int _nameLength;
        private int _nameStart;
        private NamespaceBinding[]? _nsBindings;
        private NamespaceBinding8 _nsBindingsInline;
        private int _nsCount;
        private int _nsDefaultIndex;
        private Int8Buffer _nsPrefixCacheIndexes;
        private Int8Buffer _nsPrefixCacheLengths;
        private int _nsPrefixCacheMask;
        private Int8Buffer _nsPrefixCacheStarts;
        private XmlReaderOptions _options;
        private int _pendingEmptyNsDepth;
        private int _pos;
        private int _prefixLength;
        private byte[][]? _resumeLocalNames;
        private int _resumeNameCount;
        private byte[][]? _resumeNsUris;
        private bool _seenRoot;
        private ReadOnlySequence<byte> _sequence;
        private byte[]? _sequenceCopy;
        private ReadOnlySpan<byte> _span;
        private long _tokenLineNumber;
        private long _tokenLineStart;
        private long _tokenStartIndex;
        private XmlTokenType _tokenType;
        private bool _valueIsEscaped;
        private int _valueLength;
        private int _valueStart;
        private XmlVersion _version;
        private bool _xmlDeclarationAllowed;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the number of attributes on the current start tag.
        /// </summary>
        public readonly int AttributeCount => _attributeCount;

        /// <summary>
        /// Gets the number of bytes consumed from the current buffer.
        /// </summary>
        public readonly long BytesConsumed => _pos;

        /// <summary>
        /// Gets the current element depth. The root element is depth 1.
        /// Empty elements do not increment depth.
        /// </summary>
        public readonly int CurrentDepth => _depth;

        /// <summary>
        /// Gets an opaque resume snapshot.
        /// </summary>
        /// <remarks>
        /// The public surface of <see cref="XmlReaderState"/> is <see cref="XmlReaderState.Options"/> only.
        /// The implementation snapshot is copied so two readers never share mutable stacks.
        /// </remarks>
        public readonly XmlReaderState CurrentState => new(_options, CaptureSnapshot());

        /// <summary>
        /// Gets a value indicating whether the current start tag has attributes.
        /// </summary>
        public readonly bool HasAttributes => _attributeCount > 0;

        /// <summary>
        /// Gets a value indicating whether the current value is a multi-segment sequence.
        /// Always <see langword="false"/>; use <see cref="ValueSpan"/>.
        /// </summary>
        public readonly bool HasValueSequence => false;

        /// <summary>
        /// Gets a value indicating whether the current start tag was an empty element (<c>&lt;e/&gt;</c>).
        /// </summary>
        public readonly bool IsEmptyElement => _isEmptyElement;

        /// <summary>
        /// Gets a value indicating whether this buffer is the last in a resume series.
        /// </summary>
        public readonly bool IsFinalBlock => _isFinalBlock;

        /// <summary>
        /// Gets the UTF-8 local name of the current element or attribute.
        /// </summary>
        public readonly ReadOnlySpan<byte> LocalNameSpan
        {
            get
            {
                if (_inAttribute)
                {
                    var a = CurrentAttribute();
                    var adj = a.PrefixLength > 0 ? a.PrefixLength + 1 : 0;
                    return _span.Slice(a.NameStart + adj, a.NameLength - adj);
                }

                var prefixAdj = _prefixLength > 0 ? _prefixLength + 1 : 0;
                return _span.Slice(_nameStart + prefixAdj, _nameLength - prefixAdj);
            }
        }

        /// <summary>
        /// Gets the UTF-8 namespace URI of the current name.
        /// Unprefixed attributes are in no namespace. Unprefixed elements use the default xmlns.
        /// </summary>
        public readonly ReadOnlySpan<byte> NamespaceUriSpan
        {
            get
            {
                if (_inAttribute)
                {
                    return ResolveAttributeNamespace(CurrentAttribute());
                }

                if (_tokenType is XmlTokenType.StartElement or XmlTokenType.EndElement)
                {
                    return CurrentElementNamespace();
                }

                return [];
            }
        }

        /// <summary>
        /// Gets the position in a sequence payload, if any.
        /// </summary>
        public readonly SequencePosition Position => _hasSequence ? _sequence.GetPosition(_pos) : default;

        /// <summary>
        /// Gets the UTF-8 prefix of the current element or attribute.
        /// </summary>
        public readonly ReadOnlySpan<byte> PrefixSpan
        {
            get
            {
                if (_inAttribute)
                {
                    var a = CurrentAttribute();
                    if (a.PrefixLength == 0)
                    {
                        return ReadOnlySpan<byte>.Empty;
                    }

                    return _span.Slice(a.NameStart, a.PrefixLength);
                }

                if (_prefixLength == 0)
                {
                    return ReadOnlySpan<byte>.Empty;
                }

                return _span.Slice(_nameStart, _prefixLength);
            }
        }

        /// <summary>
        /// Gets the byte offset of the current token in the document.
        /// </summary>
        public readonly long TokenStartIndex => _tokenStartIndex;

        /// <summary>
        /// Gets the current token type.
        /// After <see cref="MoveToNextAttribute"/> this is <see cref="XmlTokenType.Attribute"/>.
        /// </summary>
        public readonly XmlTokenType TokenType => _inAttribute ? XmlTokenType.Attribute : _tokenType;

        /// <summary>
        /// Gets a value indicating whether the current value differs from its bytes in the input: it held
        /// character or entity references, or line ends that were normalized.
        /// </summary>
        public readonly bool ValueIsEscaped => _inAttribute ? CurrentAttribute().ValueIsEscaped : _valueIsEscaped;

        /// <summary>
        /// Gets a multi-segment value. Always empty; use <see cref="ValueSpan"/>.
        /// </summary>
        public readonly ReadOnlySequence<byte> ValueSequence => ReadOnlySequence<byte>.Empty;

        /// <summary>
        /// Gets the UTF-8 bytes of the current token value, decoded if entities were present.
        /// </summary>
        public readonly ReadOnlySpan<byte> ValueSpan
        {
            get
            {
                if (_inAttribute)
                {
                    var a = CurrentAttribute();
                    return a.DecodedStart >= 0
                        ? _attributeDecoded.AsSpan(a.DecodedStart, a.DecodedLength)
                        : _span.Slice(a.ValueStart, a.ValueLength);
                }

                if (_valueIsEscaped && _decodedValue is { } buffer)
                {
                    return buffer.AsSpan(0, _decodedLength);
                }

                return _span.Slice(_valueStart, _valueLength);
            }
        }

        /// <summary>
        /// Gets the XML version in force after the declaration, or <see cref="XmlVersion.Version10"/> if omitted.
        /// </summary>
        public readonly XmlVersion Version => _version;

        #endregion

        #region Constructors

        /// <summary>
        /// Reads a complete UTF-8 XML payload with default options
        /// (<see cref="XmlCommentHandling.Skip"/>, insignificant whitespace).
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 XML bytes. An optional BOM is consumed.</param>
        public Utf8XmlReader(ReadOnlySpan<byte> utf8Xml)
            : this(utf8Xml, new XmlReaderOptions())
        {
        }

        /// <summary>
        /// Reads a complete UTF-8 XML payload.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 XML bytes. An optional BOM is consumed.</param>
        /// <param name="options">Reader options. Pass <c>new XmlReaderOptions()</c> for documented defaults.</param>
        public Utf8XmlReader(ReadOnlySpan<byte> utf8Xml, XmlReaderOptions options)
            : this(utf8Xml, isFinalBlock: true, new XmlReaderState(options))
        {
        }

        /// <summary>
        /// Reads a UTF-8 XML payload, optionally resuming from <paramref name="state"/>.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 XML bytes.</param>
        /// <param name="isFinalBlock">Whether this buffer is the last. Incomplete tokens throw when true.</param>
        /// <param name="state">Resume state from <see cref="CurrentState"/>, or default to start fresh.</param>
        public Utf8XmlReader(ReadOnlySpan<byte> utf8Xml, bool isFinalBlock, XmlReaderState state)
            : this(utf8Xml, sequenceCopy: null, sequence: default, hasSequence: false, isFinalBlock, state)
        {
        }

        /// <summary>
        /// Reads a complete UTF-8 XML sequence with default options.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 XML bytes. Multi-segment sequences are copied into a contiguous buffer.</param>
        public Utf8XmlReader(ReadOnlySequence<byte> utf8Xml)
            : this(utf8Xml, new XmlReaderOptions())
        {
        }

        /// <summary>
        /// Reads a complete UTF-8 XML sequence.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 XML bytes. Multi-segment sequences are copied into a contiguous buffer.</param>
        /// <param name="options">Reader options.</param>
        public Utf8XmlReader(ReadOnlySequence<byte> utf8Xml, XmlReaderOptions options)
            : this(utf8Xml, isFinalBlock: true, new XmlReaderState(options))
        {
        }

        /// <summary>
        /// Reads a UTF-8 XML sequence, optionally resuming from <paramref name="state"/>.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 XML bytes.</param>
        /// <param name="isFinalBlock">Whether this buffer is the last.</param>
        /// <param name="state">Resume state from <see cref="CurrentState"/>, or default to start fresh.</param>
        public Utf8XmlReader(ReadOnlySequence<byte> utf8Xml, bool isFinalBlock, XmlReaderState state)
        {
            if (utf8Xml.IsSingleSegment)
            {
                this = new Utf8XmlReader(utf8Xml.FirstSpan, isFinalBlock, state);
                return;
            }

            var copy = utf8Xml.ToArray();
            this = new Utf8XmlReader(copy, copy, utf8Xml, hasSequence: true, isFinalBlock, state);
        }

        /// <summary>
        /// Shared constructor used by the span and sequence overloads.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 buffer to tokenize. Multi-segment sequences are copied first.</param>
        /// <param name="sequenceCopy">The owned copy of a multi-segment sequence, or null.</param>
        /// <param name="sequence">The original sequence, used only for <see cref="Position"/>.</param>
        /// <param name="hasSequence">True when the caller supplied a <see cref="ReadOnlySequence{T}"/>.</param>
        /// <param name="isFinalBlock">Whether this buffer is the last in a resume series.</param>
        /// <param name="state">Resume state, or default to start a new document.</param>
        /// <remarks>
        /// Applies <paramref name="state"/> then consumes an optional UTF-8 BOM at offset 0.
        /// </remarks>
        internal Utf8XmlReader(ReadOnlySpan<byte> utf8Xml, byte[]? sequenceCopy, ReadOnlySequence<byte> sequence, bool hasSequence, bool isFinalBlock, XmlReaderState state)
        {
            _attributeCount = 0;
            _attributeIndex = -1;
            _attributes = null;
            _attributeDecoded = null;
            _attributeDecodedLength = 0;
            // The inline slots hold no object references and each is written before it is read (a count or a
            // mask guards every read), so they are left as they are. Clearing them was most of the cost of
            // constructing a reader. The two element-namespace stacks are the exception: a slot there is
            // written only once a namespace is in scope and read as "no namespace" before that.
            Unsafe.SkipInit(out _attributesInline);
            _currentNsLength = 0;
            _currentNsOwned = XmlUtf8.Empty;
            _currentNsStart = -1;
            _decodedLength = 0;
            _decodedValue = null;
            _depth = 0;
            _documentBytesConsumedBase = 0;
            _elementCount = 0;
            _elementNsLengths = null;
            _elementNsLengthsInline = default;
            _elementNsStarts = null;
            _elementNsStartsInline = default;
            _elementNsStored = false;
            _elementNsUris = null;
            _elementNameLengths = null;
            Unsafe.SkipInit(out _elementNameLengthsInline);
            _elementNameStarts = null;
            Unsafe.SkipInit(out _elementNameStartsInline);
            _elementPrefixLengths = null;
            Unsafe.SkipInit(out _elementPrefixLengthsInline);
            _finished = false;
            _hasNamespaceAttribute = false;
            _hasUnresolvedAttribute = false;
            _hasXmlSpaceAttribute = false;
            _hasSequence = hasSequence;
            _inAttribute = false;
            _incomplete = false;
            _internedUriCount = 0;
            _internedUris = null;
            _isEmptyElement = false;
            _isFinalBlock = isFinalBlock;
            _lineNumber = 1;
            _lineStart = 0;
            _maxAttributes = XmlLimits.Resolve(state.Options.MaxAttributeCount, XmlLimits.DefaultMaxAttributeCount);
            _maxCommentBytes = XmlLimits.Resolve(state.Options.MaxCommentByteCount, XmlLimits.DefaultMaxCommentByteCount);
            _maxDepth = XmlLimits.Resolve(state.Options.MaxDepth, XmlLimits.DefaultMaxDepth);
            _maxNameBytes = XmlLimits.Resolve(state.Options.MaxNameByteCount, XmlLimits.DefaultMaxNameByteCount);
            _maxTextBytes = XmlLimits.Resolve(state.Options.MaxTextByteCount, XmlLimits.DefaultMaxTextByteCount);
            _nameLength = 0;
            _nameStart = 0;
            _nsBindings = null;
            Unsafe.SkipInit(out _nsBindingsInline);
            _nsCount = 0;
            _nsDefaultIndex = NsIndexUnknown;
            Unsafe.SkipInit(out _nsPrefixCacheIndexes);
            Unsafe.SkipInit(out _nsPrefixCacheLengths);
            _nsPrefixCacheMask = 0;
            Unsafe.SkipInit(out _nsPrefixCacheStarts);
            _options = state.Options;
            _pendingEmptyNsDepth = 0;
            _pos = 0;
            _prefixLength = 0;
            _resumeLocalNames = null;
            _resumeNameCount = 0;
            _resumeNsUris = null;
            _seenRoot = false;
            _sequence = sequence;
            _sequenceCopy = sequenceCopy;
            _span = utf8Xml;
            _tokenLineNumber = 1;
            _tokenLineStart = 0;
            _tokenStartIndex = 0;
            _tokenType = XmlTokenType.None;
            _valueIsEscaped = false;
            _valueLength = 0;
            _valueStart = 0;
            _version = XmlVersion.Version10;
            _xmlDeclarationAllowed = true;
            ApplySnapshot(state.Snapshot);
            SkipBomIfAtStart();
            var maxDocument = _options.MaxDocumentByteCount;
            if (maxDocument > 0 && _documentBytesConsumedBase + _span.Length > maxDocument)
            {
                throw Error($"{nameof(XmlReaderOptions.MaxDocumentByteCount)} exceeded ({maxDocument}).");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Copies the current value as UTF-8 into <paramref name="utf8Destination"/>.
        /// </summary>
        /// <param name="utf8Destination">The destination buffer.</param>
        /// <returns>The number of bytes written.</returns>
        public readonly int CopyString(Span<byte> utf8Destination)
        {
            var value = ValueSpan;
            value.CopyTo(utf8Destination);
            return value.Length;
        }

        /// <summary>
        /// Copies the current value as UTF-16 into <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">The destination buffer.</param>
        /// <returns>The number of characters written.</returns>
        public readonly int CopyString(Span<char> destination)
        {
            return Encoding.UTF8.GetChars(ValueSpan, destination);
        }

        /// <summary>
        /// Reads the current text, CDATA, or attribute value as an XSD boolean.
        /// </summary>
        /// <returns>The Boolean value.</returns>
        /// <exception cref="XmlException">The value is not <c>true</c>, <c>false</c>, <c>1</c>, or <c>0</c>.</exception>
        public bool GetBoolean()
        {
            if (!XmlLexical.TryParseBoolean(ValueSpan, out var value))
            {
                throw Error("Invalid boolean.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as an unsigned 8-bit integer.
        /// </summary>
        /// <returns>The parsed value.</returns>
        /// <exception cref="XmlException">The lexical form is not an XSD integer, or the value overflows.</exception>
        public byte GetByte()
        {
            if (!TryGetByte(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as XSD <c>base64Binary</c>.
        /// </summary>
        /// <returns>The decoded bytes.</returns>
        /// <exception cref="XmlException">The value is not valid Base64.</exception>
        public byte[] GetBytesFromBase64()
        {
            if (!TryGetBytesFromBase64(out var value))
            {
                throw Error("Invalid base64.");
            }

            return value;
        }

        /// <summary>
        /// Reads the payload of the current <see cref="XmlTokenType.Comment"/> token.
        /// </summary>
        /// <returns>The comment text, without the surrounding <c>&lt;!-- --&gt;</c> delimiters.</returns>
        /// <exception cref="XmlException">The current token is not a comment.</exception>
        public string GetComment()
        {
            if (TokenType != XmlTokenType.Comment)
            {
                throw Error("Current token is not a comment.");
            }

            return CurrentValueText();
        }

        /// <summary>
        /// Reads the current value as XSD <c>date</c>.
        /// </summary>
        /// <returns>The parsed date.</returns>
        /// <exception cref="XmlException">The value is not a valid <c>xs:date</c>.</exception>
        public DateOnly GetDateOnly()
        {
            if (!TryGetDateOnly(out var value))
            {
                throw Error("Invalid xs:date.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as XSD <c>dateTime</c>.
        /// </summary>
        /// <returns>The parsed date and time.</returns>
        /// <exception cref="XmlException">The value is not a valid <c>xs:dateTime</c>.</exception>
        public DateTime GetDateTime()
        {
            if (!TryGetDateTime(out var value))
            {
                throw Error("Invalid xs:dateTime.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as XSD <c>dateTime</c> with an offset.
        /// </summary>
        /// <returns>The parsed date, time, and offset.</returns>
        /// <exception cref="XmlException">The value is not a valid <c>xs:dateTime</c>.</exception>
        public DateTimeOffset GetDateTimeOffset()
        {
            if (!TryGetDateTimeOffset(out var value))
            {
                throw Error("Invalid xs:dateTime.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as XSD <c>decimal</c>. Exponent notation is rejected.
        /// </summary>
        /// <returns>The parsed value.</returns>
        /// <exception cref="XmlException">The value is not a valid XSD decimal.</exception>
        public decimal GetDecimal()
        {
            if (!TryGetDecimal(out var value))
            {
                throw Error("Invalid decimal.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as XSD <c>double</c>, including <c>INF</c>, <c>-INF</c>, and <c>NaN</c>.
        /// </summary>
        /// <returns>The parsed value.</returns>
        /// <exception cref="XmlException">The value is not a valid double.</exception>
        public double GetDouble()
        {
            if (!TryGetDouble(out var value))
            {
                throw Error("Invalid double.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as a GUID.
        /// </summary>
        /// <returns>The parsed GUID.</returns>
        /// <exception cref="XmlException">The value is not a GUID in a supported format.</exception>
        public Guid GetGuid()
        {
            if (!TryGetGuid(out var value))
            {
                throw Error("Invalid guid.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as a 16-bit integer.
        /// </summary>
        /// <returns>The parsed value.</returns>
        /// <exception cref="XmlException">The lexical form is not an XSD integer, or the value overflows.</exception>
        public short GetInt16()
        {
            if (!TryGetInt16(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as a 32-bit integer.
        /// </summary>
        /// <returns>The parsed value.</returns>
        /// <exception cref="XmlException">The lexical form is not an XSD integer, or the value overflows.</exception>
        public int GetInt32()
        {
            if (!TryGetInt32(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as a 64-bit integer.
        /// </summary>
        /// <returns>The parsed value.</returns>
        /// <exception cref="XmlException">The lexical form is not an XSD integer, or the value overflows.</exception>
        public long GetInt64()
        {
            if (!TryGetInt64(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Returns true when the current start element has <c>xsi:nil="true"</c> or <c>xsi:nil="1"</c>.
        /// </summary>
        /// <returns><see langword="true"/> if the element is nil.</returns>
        /// <exception cref="XmlException">The current token is not a start element.</exception>
        /// <remarks>
        /// The local name <c>nil</c> must be in the XML Schema instance namespace.
        /// An unprefixed <c>nil</c> attribute is in no namespace and does not count.
        /// </remarks>
        public bool GetNil()
        {
            if (_tokenType != XmlTokenType.StartElement)
            {
                throw Error("Current token is not a start element.");
            }

            var saved = _attributeIndex;
            var savedIn = _inAttribute;
            try
            {
                _attributeIndex = -1;
                while (MoveToNextAttribute())
                {
                    if (LocalNameEquals(XmlAttributes.Nil)
                        && NamespaceUriEquals(XmlNamespaces.Xsi))
                    {
                        return XmlLexical.TryParseBoolean(ValueSpan, out var nil) && nil;
                    }
                }

                return false;
            }
            finally
            {
                _attributeIndex = saved;
                _inAttribute = savedIn;
            }
        }

        /// <summary>
        /// Reads the current value as a signed 8-bit integer.
        /// </summary>
        /// <returns>The parsed value.</returns>
        /// <exception cref="XmlException">The lexical form is not an XSD integer, or the value overflows.</exception>
        [CLSCompliant(false)]
        public sbyte GetSByte()
        {
            if (!TryGetSByte(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as XSD <c>float</c>, including <c>INF</c>, <c>-INF</c>, and <c>NaN</c>.
        /// </summary>
        /// <returns>The parsed value.</returns>
        /// <exception cref="XmlException">The value is not a valid float.</exception>
        public float GetSingle()
        {
            if (!TryGetSingle(out var value))
            {
                throw Error("Invalid float.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current token value as a UTF-16 string, after entity expansion.
        /// </summary>
        /// <returns>The decoded string. Never null; empty when the value is empty.</returns>
        public string? GetString()
        {
            return CurrentValueText();
        }

        /// <summary>
        /// Reads the current value as XSD <c>time</c>.
        /// </summary>
        /// <returns>The parsed time of day.</returns>
        /// <exception cref="XmlException">The value is not a valid <c>xs:time</c>.</exception>
        public TimeOnly GetTimeOnly()
        {
            if (!TryGetTimeOnly(out var value))
            {
                throw Error("Invalid xs:time.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as XSD <c>duration</c>. Year and month components are rejected.
        /// </summary>
        /// <returns>The parsed duration.</returns>
        /// <exception cref="XmlException">The value is not a duration that can be represented as <see cref="TimeSpan"/>.</exception>
        public TimeSpan GetTimeSpan()
        {
            if (!TryGetTimeSpan(out var value))
            {
                throw Error("Invalid xs:duration.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as an unsigned 16-bit integer.
        /// </summary>
        /// <returns>The parsed value.</returns>
        /// <exception cref="XmlException">The lexical form is not an XSD integer, or the value overflows.</exception>
        [CLSCompliant(false)]
        public ushort GetUInt16()
        {
            if (!TryGetUInt16(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as an unsigned 32-bit integer.
        /// </summary>
        /// <returns>The parsed value.</returns>
        /// <exception cref="XmlException">The lexical form is not an XSD integer, or the value overflows.</exception>
        [CLSCompliant(false)]
        public uint GetUInt32()
        {
            if (!TryGetUInt32(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current value as an unsigned 64-bit integer.
        /// </summary>
        /// <returns>The parsed value.</returns>
        /// <exception cref="XmlException">The lexical form is not an XSD integer, or the value overflows.</exception>
        [CLSCompliant(false)]
        public ulong GetUInt64()
        {
            if (!TryGetUInt64(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Compares the current local name to UTF-8 <paramref name="utf8LocalName"/>.
        /// </summary>
        /// <param name="utf8LocalName">The UTF-8 local name.</param>
        /// <returns><see langword="true"/> if they are equal.</returns>
        public readonly bool LocalNameEquals(ReadOnlySpan<byte> utf8LocalName)
        {
            return LocalNameSpan.SequenceEqual(utf8LocalName);
        }

        /// <summary>
        /// Compares the current local name to <paramref name="localName"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <returns><see langword="true"/> if they are equal.</returns>
        public readonly bool LocalNameEquals(ReadOnlySpan<char> localName)
        {
            Span<byte> utf8 = stackalloc byte[Encoding.UTF8.GetMaxByteCount(localName.Length)];
            var n = Encoding.UTF8.GetBytes(localName, utf8);
            return LocalNameSpan.SequenceEqual(utf8[..n]);
        }

        /// <summary>
        /// Compares the current local name to <paramref name="localName"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <returns><see langword="true"/> if they are equal.</returns>
        public readonly bool LocalNameEquals(string? localName)
        {
            return localName is not null && LocalNameEquals(localName.AsSpan());
        }

        /// <summary>
        /// Moves back to the start element from the current attribute.
        /// </summary>
        public void MoveToElement()
        {
            _inAttribute = false;
            _attributeIndex = -1;
        }

        /// <summary>
        /// Moves to the next attribute on the current start tag.
        /// </summary>
        /// <returns><see langword="true"/> if an attribute was selected; otherwise <see langword="false"/>.</returns>
        public bool MoveToNextAttribute()
        {
            if (_tokenType != XmlTokenType.StartElement && !_inAttribute)
            {
                return false;
            }

            if (_attributeIndex + 1 >= _attributeCount)
            {
                return false;
            }

            _attributeIndex++;
            _inAttribute = true;
            return true;
        }

        /// <summary>
        /// Compares the current namespace URI to UTF-8 <paramref name="utf8NamespaceUri"/>.
        /// </summary>
        /// <param name="utf8NamespaceUri">The UTF-8 namespace URI.</param>
        /// <returns><see langword="true"/> if they are equal.</returns>
        public readonly bool NamespaceUriEquals(ReadOnlySpan<byte> utf8NamespaceUri)
        {
            return NamespaceUriSpan.SequenceEqual(utf8NamespaceUri);
        }

        /// <summary>
        /// Compares the current namespace URI to <paramref name="namespaceUri"/>.
        /// </summary>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <returns><see langword="true"/> if they are equal.</returns>
        public readonly bool NamespaceUriEquals(ReadOnlySpan<char> namespaceUri)
        {
            if (namespaceUri.IsEmpty)
            {
                return NamespaceUriSpan.IsEmpty;
            }

            Span<byte> utf8 = stackalloc byte[Encoding.UTF8.GetMaxByteCount(namespaceUri.Length)];
            var n = Encoding.UTF8.GetBytes(namespaceUri, utf8);
            return NamespaceUriSpan.SequenceEqual(utf8[..n]);
        }

        /// <summary>
        /// Compares the current namespace URI to <paramref name="namespaceUri"/>.
        /// </summary>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <returns><see langword="true"/> if they are equal.</returns>
        public readonly bool NamespaceUriEquals(string? namespaceUri)
        {
            return NamespaceUriEquals((namespaceUri ?? "").AsSpan());
        }

        /// <summary>
        /// Advances to the next token.
        /// </summary>
        /// <returns>
        /// <see langword="false"/> at the end of a complete document, or when more data is required
        /// and <see cref="IsFinalBlock"/> is <see langword="false"/>.
        /// </returns>
        /// <exception cref="XmlException">The document is not well-formed, a DoS limit was exceeded, or the buffer ended inside a token.</exception>
        public bool Read()
        {
            _inAttribute = false;
            _attributeIndex = -1;
            _isEmptyElement = false;
            _decodedLength = 0;
            _valueIsEscaped = false;
            _incomplete = false;
            if (_pendingEmptyNsDepth > 0)
            {
                PopNamespaceScope(_pendingEmptyNsDepth);
                _pendingEmptyNsDepth = 0;
            }

            if (_finished)
            {
                return false;
            }

            if (_depth > 0)
            {
                return ReadElementContent();
            }

            while (true)
            {
                if (_depth == 0)
                {
                    if (_pos < _span.Length && XmlChars.IsWhiteSpace(_span[_pos]))
                    {
                        _xmlDeclarationAllowed = false;
                        SkipWhitespaceBytes();
                    }
                }

                if (_pos >= _span.Length)
                {
                    if (!_isFinalBlock)
                    {
                        return NeedMore(rewindToTokenStart: false);
                    }

                    _finished = true;
                    _tokenType = XmlTokenType.None;
                    if (!_seenRoot && !_options.AllowMultipleRoots)
                    {
                        throw Error("Document does not have a root element.");
                    }

                    return false;
                }

                _tokenStartIndex = _documentBytesConsumedBase + _pos;
                _tokenLineNumber = _lineNumber;
                _tokenLineStart = _lineStart;
                if (_span[_pos] is not (byte)'<')
                {
                    throw Error("Unexpected content outside the root element.");
                }

                if (_pos + 1 >= _span.Length)
                {
                    return NeedMore();
                }

                var next = _span[_pos + 1];
                if (next is (byte)'/')
                {
                    return ReadEndElement();
                }

                if (next is (byte)'!')
                {
                    if (!TryReadMarkupDeclaration())
                    {
                        return false;
                    }

                    if (_tokenType == XmlTokenType.Comment)
                    {
                        if (_options.CommentHandling is XmlCommentHandling.Skip)
                        {
                            _xmlDeclarationAllowed = false;
                            continue;
                        }

                        if (_options.CommentHandling is XmlCommentHandling.Disallow)
                        {
                            throw Error("Comments are not allowed.");
                        }
                    }

                    _xmlDeclarationAllowed = false;
                    return true;
                }

                if (next is (byte)'?')
                {
                    if (!ReadPiOrDeclaration())
                    {
                        return false;
                    }

                    _xmlDeclarationAllowed = false;
                    return true;
                }

                if (!ReadStartElement())
                {
                    return false;
                }

                _xmlDeclarationAllowed = false;
                return true;
            }
        }

        /// <summary>
        /// Skips the current element and its children.
        /// On an empty element this is a no-op; the next <see cref="Read"/> moves to the following sibling.
        /// </summary>
        /// <exception cref="XmlException">The remaining buffer is incomplete and <see cref="IsFinalBlock"/> is <see langword="false"/>.</exception>
        public void Skip()
        {
            if (!TrySkip())
            {
                throw Error("Cannot skip: more data is required.");
            }
        }

        /// <summary>
        /// Attempts to read the current value as an unsigned 8-bit integer (<c>xs:unsignedByte</c>).
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        public readonly bool TryGetByte(out byte value)
        {
            return XmlLexical.TryParseByte(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>base64Binary</c>.
        /// </summary>
        /// <param name="value">When this method returns, the decoded bytes.</param>
        /// <returns><see langword="true"/> if decoding succeeded.</returns>
        public readonly bool TryGetBytesFromBase64([NotNullWhen(true)] out byte[]? value)
        {
            return XmlLexical.TryDecodeBase64(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>date</c>.
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        public readonly bool TryGetDateOnly(out DateOnly value)
        {
            return XmlLexical.TryParseDateOnly(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>dateTime</c>.
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        public readonly bool TryGetDateTime(out DateTime value)
        {
            return XmlLexical.TryParseDateTime(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>dateTime</c> with an offset.
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        public readonly bool TryGetDateTimeOffset(out DateTimeOffset value)
        {
            return XmlLexical.TryParseDateTimeOffset(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>decimal</c>. Exponent notation is rejected.
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        public readonly bool TryGetDecimal(out decimal value)
        {
            return XmlLexical.TryParseDecimal(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>double</c>, including <c>INF</c>, <c>-INF</c>, and <c>NaN</c>.
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        public readonly bool TryGetDouble(out double value)
        {
            return XmlLexical.TryParseDouble(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as a GUID.
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        public readonly bool TryGetGuid(out Guid value)
        {
            return XmlLexical.TryParseGuid(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as a 16-bit integer (<c>xs:short</c>).
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        public readonly bool TryGetInt16(out short value)
        {
            return XmlLexical.TryParseInt16(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as a 32-bit integer (<c>xs:int</c>).
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        public readonly bool TryGetInt32(out int value)
        {
            return XmlLexical.TryParseInt32(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as a 64-bit integer (<c>xs:long</c>).
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        public readonly bool TryGetInt64(out long value)
        {
            return XmlLexical.TryParseInt64(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as a signed 8-bit integer (<c>xs:byte</c>).
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        [CLSCompliant(false)]
        public readonly bool TryGetSByte(out sbyte value)
        {
            return XmlLexical.TryParseSByte(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>float</c>.
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        public readonly bool TryGetSingle(out float value)
        {
            return XmlLexical.TryParseSingle(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>time</c>.
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        public readonly bool TryGetTimeOnly(out TimeOnly value)
        {
            return XmlLexical.TryParseTimeOnly(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>duration</c>. Year and month components are rejected.
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        public readonly bool TryGetTimeSpan(out TimeSpan value)
        {
            return XmlLexical.TryParseDuration(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as an unsigned 16-bit integer (<c>xs:unsignedShort</c>).
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        [CLSCompliant(false)]
        public readonly bool TryGetUInt16(out ushort value)
        {
            return XmlLexical.TryParseUInt16(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as an unsigned 32-bit integer (<c>xs:unsignedInt</c>).
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        [CLSCompliant(false)]
        public readonly bool TryGetUInt32(out uint value)
        {
            return XmlLexical.TryParseUInt32(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as an unsigned 64-bit integer (<c>xs:unsignedLong</c>).
        /// </summary>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        [CLSCompliant(false)]
        public readonly bool TryGetUInt64(out ulong value)
        {
            return XmlLexical.TryParseUInt64(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to skip the current element and its children.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the element was skipped;
        /// <see langword="false"/> if more data is required.
        /// </returns>
        /// <remarks>
        /// When the element is not all in the current block, the reader is put back on its start tag, as
        /// <c>Utf8JsonReader.TrySkip</c> does, so the caller can supply more data and skip or read it again.
        /// </remarks>
        public bool TrySkip()
        {
            if (_tokenType != XmlTokenType.StartElement)
            {
                return Read();
            }

            if (_isEmptyElement)
            {
                return true;
            }

            if (_isFinalBlock)
            {
                SkipChildren();
                return true;
            }

            return TrySkipPartial();
        }

        /// <summary>
        /// Compares the current value to UTF-8 <paramref name="utf8Text"/>.
        /// </summary>
        /// <param name="utf8Text">The UTF-8 text.</param>
        /// <returns><see langword="true"/> if they are equal.</returns>
        public readonly bool ValueTextEquals(ReadOnlySpan<byte> utf8Text)
        {
            return ValueSpan.SequenceEqual(utf8Text);
        }

        /// <summary>
        /// Compares the current value to <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><see langword="true"/> if they are equal.</returns>
        public readonly bool ValueTextEquals(ReadOnlySpan<char> text)
        {
            var max = Encoding.UTF8.GetMaxByteCount(text.Length);
            Span<byte> utf8 = max <= 512 ? stackalloc byte[max] : new byte[max];
            var n = Encoding.UTF8.GetBytes(text, utf8);
            return ValueSpan.SequenceEqual(utf8[..n]);
        }

        /// <summary>
        /// Compares the current value to <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><see langword="true"/> if they are equal.</returns>
        public readonly bool ValueTextEquals(string? text)
        {
            return text is not null && ValueTextEquals(text.AsSpan());
        }

        #endregion

    }

}
