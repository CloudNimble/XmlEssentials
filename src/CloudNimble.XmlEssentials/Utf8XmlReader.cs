using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Provides a high-performance API for forward-only, read-only access to UTF-8 encoded XML text.
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
    /// <para>
    /// The reader is a <see langword="ref"/> struct that reads the text one token at a time, in the manner
    /// of <c>Utf8JsonReader</c>.
    /// </para>
    /// <para>
    /// Empty elements are reported as <see cref="XmlTokenType.StartElement"/> with
    /// <see cref="IsEmptyElement"/> set to <see langword="true"/>. The reader does not synthesize an
    /// <see cref="XmlTokenType.EndElement"/> token. Callers must check
    /// <see cref="IsEmptyElement"/> before expecting a matching end tag.
    /// </para>
    /// <para>
    /// The <c>xmlns</c> attributes of a start tag bind before the element name is resolved. Unprefixed
    /// attributes are in no namespace. Unprefixed elements use the default namespace.
    /// The XML declaration, when present, must be the first token in the document (after an optional BOM).
    /// </para>
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
        /// Binding start that names the XML Schema instance namespace.
        /// </summary>
        internal const int OwnedXsi = -4;

        /// <summary>
        /// Slot flag: a child element has started inside this element.
        /// </summary>
        internal const int ElementHasChild = 1 << 29;

        /// <summary>
        /// How many bytes of a text node <see cref="ReadText"/> reads one at a time before it switches to
        /// vectorized searches.
        /// </summary>
        internal const int ShortTextBytes = 8;

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
        private Span<byte> _attributeDecoded;
        private int _attributeDecodedLength;
        private Span<AttributeRecord> _attributes;
        private AttributeRecord8 _attributesInline;
        private int _currentNsLength;
        private byte[] _currentNsOwned;
        private int _currentNsStart;
        private int _decodedLength;
        private Span<byte> _decodedValue;
        private int _depth;
        private long _documentBytesConsumedBase;
        /// <summary>
        /// Inline slots for the element-name and namespace-binding stacks. Documents shallower than this
        /// allocate no heap arrays for those stacks.
        /// </summary>
        internal const int InlineSlots = 8;

        private int _elementCount;
        private Span<int> _elementNsLengths;
        private Int8Buffer _elementNsLengthsInline;
        private Span<int> _elementNsStarts;
        private Int8Buffer _elementNsStartsInline;
        private bool _elementNsStored;
        private byte[][]? _elementNsUris;
        private Span<int> _elementNameLengths;
        private Int8Buffer _elementNameLengthsInline;
        private Span<int> _elementNameStarts;
        private Int8Buffer _elementNameStartsInline;
        private Span<int> _elementPrefixLengths;
        private Int8Buffer _elementPrefixLengthsInline;
        private bool _finished;
        private Span<byte> _scratch;
        private int _scratchUsed;
        private bool _hasNamespaceAttribute;
        private bool _hasUnresolvedAttribute;
        private bool _hasXmlSpaceAttribute;
        private readonly bool _hasSequence;
        private bool _inAttribute;
        private bool _incomplete;
        private int _internedUriCount;
        private byte[][]? _internedUris;
        private bool _isEmptyElement;
        private bool _isOnOpenStartTag;
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
        private Span<NamespaceBinding> _nsBindings;
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
        private byte[][]? _resumePrefixes;
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
        /// <value>
        /// The number of attributes on the current start tag, including namespace declarations.
        /// </value>
        public readonly int AttributeCount => _attributeCount;

        /// <summary>
        /// Gets the total number of bytes consumed so far by this instance of the <see cref="Utf8XmlReader"/>.
        /// </summary>
        /// <value>
        /// The number of bytes consumed from the current buffer.
        /// </value>
        public readonly long BytesConsumed => _pos;

        /// <summary>
        /// Gets the depth of the current token.
        /// </summary>
        /// <value>
        /// The number of elements that the current token is inside.
        /// </value>
        /// <example>
        /// <code>
        /// // &lt;a&gt;&lt;b/&gt;&lt;c&gt;text&lt;/c&gt;&lt;/a&gt;
        /// // &lt;a&gt; 0, &lt;b/&gt; 1, &lt;c&gt; 1, text 2, &lt;/c&gt; 1, &lt;/a&gt; 0
        /// </code>
        /// </example>
        /// <remarks>
        /// The root element is at depth 0, and so is anything before or after it. A start tag, its end tag, and an
        /// empty element in the same place are at the same depth, and the content of an element is one deeper.
        /// An attribute is at the depth of its element. This is the depth that <c>XmlReader.Depth</c> and
        /// <c>Utf8JsonReader.CurrentDepth</c> report.
        /// </remarks>
        public readonly int CurrentDepth => _isOnOpenStartTag ? _depth - 1 : _depth;

        /// <summary>
        /// Gets the current <see cref="Utf8XmlReader"/> state to pass to a <see cref="Utf8XmlReader"/>
        /// constructor with more data.
        /// </summary>
        /// <value>
        /// The state to resume reading from.
        /// </value>
        /// <remarks>
        /// The state is opaque. The public surface of <see cref="XmlReaderState"/> is
        /// <see cref="XmlReaderState.Options"/> only. The reader's progress is copied into the state, so two
        /// readers never share mutable stacks.
        /// </remarks>
        public readonly XmlReaderState CurrentState => new(_options, CaptureSnapshot());

        /// <summary>
        /// Gets a value that indicates whether the current start tag has attributes.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the current start tag has at least one attribute; otherwise,
        /// <see langword="false"/>.
        /// </value>
        public readonly bool HasAttributes => _attributeCount > 0;

        /// <summary>
        /// Gets a value that indicates whether the current start tag is an empty element (<c>&lt;e/&gt;</c>).
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the current start tag is an empty element; otherwise,
        /// <see langword="false"/>.
        /// </value>
        public readonly bool IsEmptyElement => _isEmptyElement;

        /// <summary>
        /// Gets a value indicating whether the bytes after the current token start an end tag.
        /// </summary>
        /// <remarks>
        /// After a text or CDATA token this tells a caller that the element holds nothing more, so the value can
        /// be used where it lies: reading the end tag does not disturb it. It is <see langword="false"/> when the
        /// current block ends before the next two bytes.
        /// </remarks>
        internal readonly bool IsEndTagNext => (uint)(_pos + 1) < (uint)_span.Length && _span[_pos] is (byte)'<' && _span[_pos + 1] is (byte)'/';

        /// <summary>
        /// Gets how many bytes at the start of the scratch buffer the reader has written to.
        /// </summary>
        internal readonly int ScratchBytesUsed => _scratchUsed;

        /// <summary>
        /// Gets a value that indicates whether all the XML data was provided or there is more data to come.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the reader was constructed with the input span or sequence containing
        /// the entire XML data to process; <see langword="false"/> if the reader was constructed with an
        /// input span or sequence that may contain partial XML data with more data to follow.
        /// </value>
        public readonly bool IsFinalBlock => _isFinalBlock;

        /// <summary>
        /// Gets the local name of the current element or attribute.
        /// </summary>
        /// <value>
        /// The local name, without a prefix, as UTF-8 bytes.
        /// </value>
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
        /// Gets the namespace URI of the current element or attribute.
        /// </summary>
        /// <value>
        /// The namespace URI as UTF-8 bytes, or an empty span if the name is in no namespace or the current
        /// token is not a start tag, an end tag, or an attribute.
        /// </value>
        /// <remarks>
        /// Unprefixed attributes are in no namespace. Unprefixed elements use the default namespace.
        /// </remarks>
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
        /// Gets the current <see cref="SequencePosition"/> within the provided UTF-8 encoded input
        /// <see cref="ReadOnlySequence{T}"/>, or a default <see cref="SequencePosition"/> if the
        /// <see cref="Utf8XmlReader"/> struct was constructed with a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        /// <value>
        /// The position in the input sequence that the reader has consumed up to, or a default
        /// <see cref="SequencePosition"/> if the input is a span.
        /// </value>
        public readonly SequencePosition Position => _hasSequence ? _sequence.GetPosition(_pos) : default;

        /// <summary>
        /// Gets the namespace prefix of the current element or attribute.
        /// </summary>
        /// <value>
        /// The prefix as UTF-8 bytes, or an empty span if the name has no prefix.
        /// </value>
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
        /// Gets the index that the last processed XML token starts at in the document.
        /// </summary>
        /// <value>
        /// The zero-based byte offset of the token from the start of the document. When the document is read
        /// in several blocks, the offset counts the bytes of the earlier blocks.
        /// </value>
        public readonly long TokenStartIndex => _tokenStartIndex;

        /// <summary>
        /// Gets the type of the last processed XML token in the UTF-8 encoded XML text.
        /// </summary>
        /// <value>
        /// One of the enumeration values that indicates the type of the current token. After
        /// <see cref="MoveToNextAttribute"/> returns <see langword="true"/>, this is
        /// <see cref="XmlTokenType.Attribute"/>.
        /// </value>
        public readonly XmlTokenType TokenType => _inAttribute ? XmlTokenType.Attribute : _tokenType;

        /// <summary>
        /// Gets a value that indicates whether the current value differs from its bytes in the input.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the value held character or entity references, or line ends that were
        /// normalized; otherwise, <see langword="false"/>.
        /// </value>
        public readonly bool ValueIsEscaped => _inAttribute ? CurrentAttribute().ValueIsEscaped : _valueIsEscaped;

        /// <summary>
        /// Gets the value of the last processed token as a read-only span of UTF-8 bytes.
        /// </summary>
        /// <value>
        /// The UTF-8 bytes of the current token value. If the value held character or entity references, or
        /// line ends that had to be normalized, the span holds the decoded value. Otherwise, it is a slice of
        /// the input.
        /// </value>
        public readonly ReadOnlySpan<byte> ValueSpan
        {
            get
            {
                if (_inAttribute)
                {
                    var a = CurrentAttribute();
                    return a.DecodedStart >= 0
                        ? _attributeDecoded.Slice(a.DecodedStart, a.DecodedLength)
                        : _span.Slice(a.ValueStart, a.ValueLength);
                }

                if (_valueIsEscaped)
                {
                    return _decodedValue[.._decodedLength];
                }

                return _span.Slice(_valueStart, _valueLength);
            }
        }

        /// <summary>
        /// Gets the XML version that is in force for the document.
        /// </summary>
        /// <value>
        /// One of the enumeration values that indicates the version named in the XML declaration. The default
        /// is <see cref="XmlVersion.Version10"/>, which applies until a declaration is read and when the
        /// document has none.
        /// </value>
        public readonly XmlVersion Version => _version;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8XmlReader"/> struct that processes a read-only
        /// span of UTF-8 encoded text that holds a complete document, using the default options.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 encoded XML text to process. An optional BOM is consumed.</param>
        /// <remarks>
        /// The default options report comments (<see cref="XmlCommentHandling.Allow"/>) and whitespace-only text
        /// between elements (<see cref="XmlWhitespaceHandling.Significant"/>) as tokens.
        /// </remarks>
        public Utf8XmlReader(ReadOnlySpan<byte> utf8Xml)
            : this(utf8Xml, new XmlReaderOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8XmlReader"/> struct that processes a read-only
        /// span of UTF-8 encoded text that holds a complete document, using the specified options.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 encoded XML text to process. An optional BOM is consumed.</param>
        /// <param name="options">The options that define the behavior of the reader.</param>
        /// <exception cref="XmlException">
        /// <paramref name="utf8Xml"/> is longer than <see cref="XmlReaderOptions.MaxDocumentByteCount"/>.
        /// </exception>
        public Utf8XmlReader(ReadOnlySpan<byte> utf8Xml, XmlReaderOptions options)
            : this(utf8Xml, isFinalBlock: true, new XmlReaderState(options))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8XmlReader"/> struct that processes a read-only
        /// span of UTF-8 encoded text and indicates whether the input contains all the text to process.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 encoded XML text to process. An optional BOM at the start of the document is consumed.</param>
        /// <param name="isFinalBlock">
        /// <see langword="true"/> to indicate that the input span contains the entire data to process;
        /// <see langword="false"/> to indicate that the input span contains partial data with more data to follow.
        /// When <see langword="true"/>, the reader throws an <see cref="XmlException"/> if the input ends inside a token.
        /// </param>
        /// <param name="state">
        /// The reader state. If this is the first call to the constructor, pass a new
        /// <see cref="XmlReaderState"/> that holds the options to use. Otherwise, pass the value of the
        /// <see cref="CurrentState"/> property from the previous instance of the <see cref="Utf8XmlReader"/>.
        /// </param>
        /// <exception cref="XmlException">
        /// The bytes read so far and the length of <paramref name="utf8Xml"/> together exceed
        /// <see cref="XmlReaderOptions.MaxDocumentByteCount"/>.
        /// </exception>
        /// <remarks>
        /// A <see langword="default"/> <see cref="XmlReaderState"/> holds the default options, which report
        /// comments and whitespace-only text between elements as tokens.
        /// </remarks>
        public Utf8XmlReader(ReadOnlySpan<byte> utf8Xml, bool isFinalBlock, XmlReaderState state)
            : this(utf8Xml, scratch: default, sequenceCopy: null, sequence: default, hasSequence: false, isFinalBlock, state)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8XmlReader"/> struct that processes a read-only
        /// span of UTF-8 encoded text that holds a complete document, using the specified options and a
        /// caller-supplied buffer as working memory.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 encoded XML text to process. An optional BOM is consumed.</param>
        /// <param name="scratch">
        /// The memory the reader uses before it allocates any. The reader may overwrite it for as long as the
        /// reader is in use. Its contents on entry do not matter.
        /// It may come from <see langword="stackalloc"/>, a pooled array, or a field reused from one reader to the next.
        /// </param>
        /// <param name="options">The options that define the behavior of the reader.</param>
        /// <exception cref="XmlException">
        /// <paramref name="utf8Xml"/> is longer than <see cref="XmlReaderOptions.MaxDocumentByteCount"/>.
        /// </exception>
        /// <example>
        /// <code>
        /// Span&lt;byte&gt; scratch = stackalloc byte[1024];
        /// var reader = new Utf8XmlReader(utf8Xml, scratch, new XmlReaderOptions());
        /// while (reader.Read())
        /// {
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// <para>
        /// A reader needs memory of its own in four cases: a value that holds a character or entity reference,
        /// or a line end other than LF, is decoded into a buffer; a start tag with more than eight attributes
        /// needs a larger attribute table; so do more than eight namespace declarations in scope; and so do
        /// elements nested more than eight deep. Without <paramref name="scratch"/> each of those is an array
        /// allocated for this reader. With it, the reader takes the memory from <paramref name="scratch"/>
        /// and allocates only once that is used up, so a buffer of any size is safe and a larger one saves more.
        /// </para>
        /// <para>
        /// Copies of the reader share the buffer, as they share the arrays a reader without one allocates.
        /// A copy may look ahead within the element the original is on and then be dropped, or the copy
        /// may be the one that carries on. Do not advance two copies in turn, and do not go back to the
        /// original once a copy has read past the end of the element the original is on.
        /// </para>
        /// </remarks>
        public Utf8XmlReader(ReadOnlySpan<byte> utf8Xml, Span<byte> scratch, XmlReaderOptions options)
            : this(utf8Xml, scratch, isFinalBlock: true, new XmlReaderState(options))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8XmlReader"/> struct that processes a read-only
        /// span of UTF-8 encoded text, indicates whether the input contains all the text to process, and
        /// uses a caller-supplied buffer as working memory.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 encoded XML text to process. An optional BOM at the start of the document is consumed.</param>
        /// <param name="scratch">
        /// The memory the reader uses before it allocates any. The reader may overwrite it for as long as the
        /// reader is in use. Its contents on entry do not matter, so
        /// the same buffer can be passed to the reader for each block of a document.
        /// </param>
        /// <param name="isFinalBlock">
        /// <see langword="true"/> to indicate that the input span contains the entire data to process;
        /// <see langword="false"/> to indicate that the input span contains partial data with more data to follow.
        /// When <see langword="true"/>, the reader throws an <see cref="XmlException"/> if the input ends inside a token.
        /// </param>
        /// <param name="state">
        /// The reader state. If this is the first call to the constructor, pass a new
        /// <see cref="XmlReaderState"/> that holds the options to use. Otherwise, pass the value of the
        /// <see cref="CurrentState"/> property from the previous instance of the <see cref="Utf8XmlReader"/>.
        /// </param>
        /// <exception cref="XmlException">
        /// The bytes read so far and the length of <paramref name="utf8Xml"/> together exceed
        /// <see cref="XmlReaderOptions.MaxDocumentByteCount"/>.
        /// </exception>
        /// <remarks>
        /// See <see cref="Utf8XmlReader(ReadOnlySpan{byte}, Span{byte}, XmlReaderOptions)"/> for what the buffer is used for.
        /// A <see langword="default"/> <see cref="XmlReaderState"/> holds the default options, which report
        /// comments and whitespace-only text between elements as tokens.
        /// </remarks>
        public Utf8XmlReader(ReadOnlySpan<byte> utf8Xml, Span<byte> scratch, bool isFinalBlock, XmlReaderState state)
            : this(utf8Xml, scratch, sequenceCopy: null, sequence: default, hasSequence: false, isFinalBlock, state)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8XmlReader"/> struct that processes a read-only
        /// sequence of UTF-8 encoded text that holds a complete document, using the default options.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 encoded XML text to process. An optional BOM is consumed. A multi-segment sequence is copied into a contiguous buffer.</param>
        /// <remarks>
        /// The default options report comments and whitespace-only text between elements as tokens.
        /// </remarks>
        public Utf8XmlReader(ReadOnlySequence<byte> utf8Xml)
            : this(utf8Xml, new XmlReaderOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8XmlReader"/> struct that processes a read-only
        /// sequence of UTF-8 encoded text that holds a complete document, using the specified options.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 encoded XML text to process. An optional BOM is consumed. A multi-segment sequence is copied into a contiguous buffer.</param>
        /// <param name="options">The options that define the behavior of the reader.</param>
        /// <exception cref="XmlException">
        /// <paramref name="utf8Xml"/> is longer than <see cref="XmlReaderOptions.MaxDocumentByteCount"/>.
        /// </exception>
        public Utf8XmlReader(ReadOnlySequence<byte> utf8Xml, XmlReaderOptions options)
            : this(utf8Xml, isFinalBlock: true, new XmlReaderState(options))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8XmlReader"/> struct that processes a read-only
        /// sequence of UTF-8 encoded text and indicates whether the input contains all the text to process.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 encoded XML text to process. An optional BOM at the start of the document is consumed. A multi-segment sequence is copied into a contiguous buffer.</param>
        /// <param name="isFinalBlock">
        /// <see langword="true"/> to indicate that the input sequence contains the entire data to process;
        /// <see langword="false"/> to indicate that the input sequence contains partial data with more data to follow.
        /// When <see langword="true"/>, the reader throws an <see cref="XmlException"/> if the input ends inside a token.
        /// </param>
        /// <param name="state">
        /// The reader state. If this is the first call to the constructor, pass a new
        /// <see cref="XmlReaderState"/> that holds the options to use. Otherwise, pass the value of the
        /// <see cref="CurrentState"/> property from the previous instance of the <see cref="Utf8XmlReader"/>.
        /// </param>
        /// <exception cref="XmlException">
        /// The bytes read so far and the length of <paramref name="utf8Xml"/> together exceed
        /// <see cref="XmlReaderOptions.MaxDocumentByteCount"/>.
        /// </exception>
        /// <remarks>
        /// A <see langword="default"/> <see cref="XmlReaderState"/> holds the default options, which report
        /// comments and whitespace-only text between elements as tokens.
        /// </remarks>
        public Utf8XmlReader(ReadOnlySequence<byte> utf8Xml, bool isFinalBlock, XmlReaderState state)
        {
            if (utf8Xml.IsSingleSegment)
            {
                // One segment is read where it lies. The sequence is kept so that Position can be answered.
                this = new Utf8XmlReader(utf8Xml.FirstSpan, scratch: default, sequenceCopy: null, utf8Xml, hasSequence: true, isFinalBlock, state);
                return;
            }

            var copy = utf8Xml.ToArray();
            this = new Utf8XmlReader(copy, scratch: default, copy, utf8Xml, hasSequence: true, isFinalBlock, state);
        }

        /// <summary>
        /// Shared constructor used by the span and sequence overloads.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 buffer to tokenize. Multi-segment sequences are copied first.</param>
        /// <param name="scratch">Working memory the reader uses before it allocates, or empty.</param>
        /// <param name="sequenceCopy">The owned copy of a multi-segment sequence, or null.</param>
        /// <param name="sequence">The original sequence, used only for <see cref="Position"/>.</param>
        /// <param name="hasSequence">True when the caller supplied a <see cref="ReadOnlySequence{T}"/>.</param>
        /// <param name="isFinalBlock">Whether this buffer is the last in a resume series.</param>
        /// <param name="state">Resume state, or default to start a new document.</param>
        /// <remarks>
        /// Applies <paramref name="state"/> then consumes an optional UTF-8 BOM at offset 0.
        /// </remarks>
        internal Utf8XmlReader(ReadOnlySpan<byte> utf8Xml, Span<byte> scratch, byte[]? sequenceCopy, ReadOnlySequence<byte> sequence, bool hasSequence, bool isFinalBlock, XmlReaderState state)
        {
            _attributeCount = 0;
            _attributeIndex = -1;
            _attributes = default;
            _attributeDecoded = default;
            _scratch = scratch;
            _scratchUsed = 0;
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
            _decodedValue = default;
            _depth = 0;
            _documentBytesConsumedBase = 0;
            _elementCount = 0;
            _elementNsLengths = default;
            _elementNsLengthsInline = default;
            _elementNsStarts = default;
            _elementNsStartsInline = default;
            _elementNsStored = false;
            _elementNsUris = null;
            _elementNameLengths = default;
            Unsafe.SkipInit(out _elementNameLengthsInline);
            _elementNameStarts = default;
            Unsafe.SkipInit(out _elementNameStartsInline);
            _elementPrefixLengths = default;
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
            _isOnOpenStartTag = false;
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
            _nsBindings = default;
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
            _resumePrefixes = null;
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
        /// Copies the current token value from the source, with references decoded, as UTF-8 text to a
        /// destination buffer.
        /// </summary>
        /// <param name="utf8Destination">A buffer to write the UTF-8 encoded value into.</param>
        /// <returns>
        /// The number of bytes written to <paramref name="utf8Destination"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="utf8Destination"/> is too small to hold the value.
        /// </exception>
        public readonly int CopyString(Span<byte> utf8Destination)
        {
            var value = ValueSpan;
            value.CopyTo(utf8Destination);
            return value.Length;
        }

        /// <summary>
        /// Copies the current token value from the source, with references decoded, as UTF-16 text to a
        /// destination buffer.
        /// </summary>
        /// <param name="destination">A buffer to write the transcoded UTF-16 characters into.</param>
        /// <returns>
        /// The number of characters written to <paramref name="destination"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="destination"/> is too small to hold the value.
        /// </exception>
        public readonly int CopyString(Span<char> destination)
        {
            return Encoding.UTF8.GetChars(ValueSpan, destination);
        }

        /// <summary>
        /// Parses the current text, CDATA, or attribute value from the source as an XSD <c>boolean</c>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the value is <c>true</c> or <c>1</c>; <see langword="false"/> if the
        /// value is <c>false</c> or <c>0</c>.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not <c>true</c>, <c>false</c>, <c>1</c>, or <c>0</c>.
        /// </exception>
        public readonly bool GetBoolean()
        {
            if (!XmlLexical.TryParseBoolean(ValueSpan, out var value))
            {
                throw Error("Invalid boolean.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="byte"/> (<c>xs:unsignedByte</c>).
        /// </summary>
        /// <returns>
        /// The value of the current token, parsed as an 8-bit unsigned integer.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not in the lexical form of an XSD integer.
        /// -or-
        /// The value is outside the range of <see cref="byte"/>.
        /// </exception>
        public readonly byte GetByte()
        {
            if (!TryGetByte(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as XSD <c>base64Binary</c> and decodes it into a
        /// byte array.
        /// </summary>
        /// <returns>
        /// The byte array that the Base64 encoded value represents.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not valid Base64.
        /// </exception>
        public readonly byte[] GetBytesFromBase64()
        {
            if (!TryGetBytesFromBase64(out var value))
            {
                throw Error("Invalid base64.");
            }

            return value;
        }

        /// <summary>
        /// Reads the content of the current <see cref="XmlTokenType.Comment"/> token from the source,
        /// transcoded as a <see cref="string"/>.
        /// </summary>
        /// <returns>
        /// The comment text, without the surrounding <c>&lt;!-- --&gt;</c> delimiters.
        /// </returns>
        /// <exception cref="XmlException">
        /// The current token is not a comment.
        /// </exception>
        public readonly string GetComment()
        {
            if (TokenType != XmlTokenType.Comment)
            {
                throw Error("Current token is not a comment.");
            }

            return CurrentValueText();
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="DateOnly"/> (<c>xs:date</c>).
        /// </summary>
        /// <returns>
        /// The date that the value of the current token represents.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not a valid <c>xs:date</c>.
        /// </exception>
        public readonly DateOnly GetDateOnly()
        {
            if (!TryGetDateOnly(out var value))
            {
                throw Error("Invalid xs:date.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="DateTime"/> (<c>xs:dateTime</c>).
        /// </summary>
        /// <returns>
        /// The date and time that the value of the current token represents.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not a valid <c>xs:dateTime</c>.
        /// </exception>
        public readonly DateTime GetDateTime()
        {
            if (!TryGetDateTime(out var value))
            {
                throw Error("Invalid xs:dateTime.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="DateTimeOffset"/>
        /// (<c>xs:dateTime</c> with an offset).
        /// </summary>
        /// <returns>
        /// The date, time, and offset that the value of the current token represents.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not a valid <c>xs:dateTime</c>.
        /// </exception>
        public readonly DateTimeOffset GetDateTimeOffset()
        {
            if (!TryGetDateTimeOffset(out var value))
            {
                throw Error("Invalid xs:dateTime.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="decimal"/> (<c>xs:decimal</c>).
        /// </summary>
        /// <returns>
        /// The value of the current token, parsed as a decimal number.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not a valid <c>xs:decimal</c>. Exponent notation is rejected.
        /// </exception>
        public readonly decimal GetDecimal()
        {
            if (!TryGetDecimal(out var value))
            {
                throw Error("Invalid decimal.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="double"/> (<c>xs:double</c>),
        /// including <c>INF</c>, <c>-INF</c>, and <c>NaN</c>.
        /// </summary>
        /// <returns>
        /// The value of the current token, parsed as a double-precision floating-point number.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not a valid <c>xs:double</c>.
        /// </exception>
        public readonly double GetDouble()
        {
            if (!TryGetDouble(out var value))
            {
                throw Error("Invalid double.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="Guid"/>.
        /// </summary>
        /// <returns>
        /// The GUID that the value of the current token represents.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not a GUID in a supported format.
        /// </exception>
        public readonly Guid GetGuid()
        {
            if (!TryGetGuid(out var value))
            {
                throw Error("Invalid guid.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="short"/> (<c>xs:short</c>).
        /// </summary>
        /// <returns>
        /// The value of the current token, parsed as a 16-bit signed integer.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not in the lexical form of an XSD integer.
        /// -or-
        /// The value is outside the range of <see cref="short"/>.
        /// </exception>
        public readonly short GetInt16()
        {
            if (!TryGetInt16(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as an <see cref="int"/> (<c>xs:int</c>).
        /// </summary>
        /// <returns>
        /// The value of the current token, parsed as a 32-bit signed integer.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not in the lexical form of an XSD integer.
        /// -or-
        /// The value is outside the range of <see cref="int"/>.
        /// </exception>
        public readonly int GetInt32()
        {
            if (!TryGetInt32(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="long"/> (<c>xs:long</c>).
        /// </summary>
        /// <returns>
        /// The value of the current token, parsed as a 64-bit signed integer.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not in the lexical form of an XSD integer.
        /// -or-
        /// The value is outside the range of <see cref="long"/>.
        /// </exception>
        public readonly long GetInt64()
        {
            if (!TryGetInt64(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Determines whether the current start element is marked nil with <c>xsi:nil="true"</c> or
        /// <c>xsi:nil="1"</c>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the element has an <c>xsi:nil</c> attribute whose value is <c>true</c> or
        /// <c>1</c>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="XmlException">
        /// The reader is not on a start element or on one of its attributes.
        /// </exception>
        /// <remarks>
        /// The local name <c>nil</c> must be in the XML Schema instance namespace.
        /// An unprefixed <c>nil</c> attribute is in no namespace and does not count.
        /// </remarks>
        public readonly bool GetNil()
        {
            if (_tokenType != XmlTokenType.StartElement)
            {
                throw Error("Current token is not a start element.");
            }

            for (var i = 0; i < _attributeCount; i++)
            {
                var attribute = AttributeAt(i);
                if (attribute.NsIndex >= AttributeRecord.FirstBinding
                    && AttributeLocalName(attribute).SequenceEqual("nil"u8)
                    && ResolveAttributeNamespace(attribute).SequenceEqual(XmlUtf8.Xsi))
                {
                    return XmlLexical.TryParseBoolean(AttributeValue(attribute), out var nil) && nil;
                }
            }

            return false;
        }

        /// <summary>
        /// Parses the current token value from the source as an <see cref="sbyte"/> (<c>xs:byte</c>).
        /// </summary>
        /// <returns>
        /// The value of the current token, parsed as an 8-bit signed integer.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not in the lexical form of an XSD integer.
        /// -or-
        /// The value is outside the range of <see cref="sbyte"/>.
        /// </exception>
        [CLSCompliant(false)]
        public readonly sbyte GetSByte()
        {
            if (!TryGetSByte(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="float"/> (<c>xs:float</c>),
        /// including <c>INF</c>, <c>-INF</c>, and <c>NaN</c>.
        /// </summary>
        /// <returns>
        /// The value of the current token, parsed as a single-precision floating-point number.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not a valid <c>xs:float</c>.
        /// </exception>
        public readonly float GetSingle()
        {
            if (!TryGetSingle(out var value))
            {
                throw Error("Invalid float.");
            }

            return value;
        }

        /// <summary>
        /// Reads the current token value from the source, with references decoded, and transcodes it as a
        /// <see cref="string"/>.
        /// </summary>
        /// <returns>
        /// The token value transcoded to a string. The result is never <see langword="null"/>. It is an empty
        /// string when the value is empty.
        /// </returns>
        public readonly string? GetString()
        {
            return CurrentValueText();
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="TimeOnly"/> (<c>xs:time</c>).
        /// </summary>
        /// <returns>
        /// The time of day that the value of the current token represents.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not a valid <c>xs:time</c>.
        /// </exception>
        public readonly TimeOnly GetTimeOnly()
        {
            if (!TryGetTimeOnly(out var value))
            {
                throw Error("Invalid xs:time.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="TimeSpan"/> (<c>xs:duration</c>).
        /// </summary>
        /// <returns>
        /// The duration that the value of the current token represents.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not a duration that can be represented as a <see cref="TimeSpan"/>. A duration with
        /// year or month components is rejected.
        /// </exception>
        public readonly TimeSpan GetTimeSpan()
        {
            if (!TryGetTimeSpan(out var value))
            {
                throw Error("Invalid xs:duration.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="ushort"/> (<c>xs:unsignedShort</c>).
        /// </summary>
        /// <returns>
        /// The value of the current token, parsed as a 16-bit unsigned integer.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not in the lexical form of an XSD integer.
        /// -or-
        /// The value is outside the range of <see cref="ushort"/>.
        /// </exception>
        [CLSCompliant(false)]
        public readonly ushort GetUInt16()
        {
            if (!TryGetUInt16(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="uint"/> (<c>xs:unsignedInt</c>).
        /// </summary>
        /// <returns>
        /// The value of the current token, parsed as a 32-bit unsigned integer.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not in the lexical form of an XSD integer.
        /// -or-
        /// The value is outside the range of <see cref="uint"/>.
        /// </exception>
        [CLSCompliant(false)]
        public readonly uint GetUInt32()
        {
            if (!TryGetUInt32(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Parses the current token value from the source as a <see cref="ulong"/> (<c>xs:unsignedLong</c>).
        /// </summary>
        /// <returns>
        /// The value of the current token, parsed as a 64-bit unsigned integer.
        /// </returns>
        /// <exception cref="XmlException">
        /// The value is not in the lexical form of an XSD integer.
        /// -or-
        /// The value is outside the range of <see cref="ulong"/>.
        /// </exception>
        [CLSCompliant(false)]
        public readonly ulong GetUInt64()
        {
            if (!TryGetUInt64(out var value))
            {
                throw Error("Invalid integer.");
            }

            return value;
        }

        /// <summary>
        /// Compares the UTF-8 encoded text in a read-only byte span to the local name of the current
        /// element or attribute and returns a value that indicates whether they match.
        /// </summary>
        /// <param name="utf8LocalName">The UTF-8 encoded text to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if the local name matches <paramref name="utf8LocalName"/> byte for byte;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public readonly bool LocalNameEquals(ReadOnlySpan<byte> utf8LocalName)
        {
            return LocalNameSpan.SequenceEqual(utf8LocalName);
        }

        /// <summary>
        /// Compares the text in a read-only character span to the local name of the current element or
        /// attribute and returns a value that indicates whether they match.
        /// </summary>
        /// <param name="localName">The text to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if the local name matches <paramref name="localName"/>; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool LocalNameEquals(ReadOnlySpan<char> localName)
        {
            Span<byte> utf8 = stackalloc byte[Encoding.UTF8.GetMaxByteCount(localName.Length)];
            var n = Encoding.UTF8.GetBytes(localName, utf8);
            return LocalNameSpan.SequenceEqual(utf8[..n]);
        }

        /// <summary>
        /// Compares the text in a string to the local name of the current element or attribute and returns
        /// a value that indicates whether they match.
        /// </summary>
        /// <param name="localName">The text to compare against. A <see langword="null"/> value matches no name.</param>
        /// <returns>
        /// <see langword="true"/> if the local name matches <paramref name="localName"/>; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool LocalNameEquals(string? localName)
        {
            return localName is not null && LocalNameEquals(localName.AsSpan());
        }

        /// <summary>
        /// Moves the reader from the current attribute back to the start element that holds it.
        /// </summary>
        /// <remarks>
        /// After this call, <see cref="MoveToNextAttribute"/> starts again at the first attribute.
        /// </remarks>
        public void MoveToElement()
        {
            _inAttribute = false;
            _attributeIndex = -1;
        }

        /// <summary>
        /// Moves the reader to the next attribute on the current start tag.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the reader moved to an attribute; <see langword="false"/> if the current
        /// token is not a start tag or the start tag has no more attributes.
        /// </returns>
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
        /// Compares the UTF-8 encoded text in a read-only byte span to the namespace URI of the current
        /// element or attribute and returns a value that indicates whether they match.
        /// </summary>
        /// <param name="utf8NamespaceUri">The UTF-8 encoded namespace URI to compare against. An empty span stands for no namespace.</param>
        /// <returns>
        /// <see langword="true"/> if the namespace URI matches <paramref name="utf8NamespaceUri"/> byte for
        /// byte; otherwise, <see langword="false"/>.
        /// </returns>
        public readonly bool NamespaceUriEquals(ReadOnlySpan<byte> utf8NamespaceUri)
        {
            return NamespaceUriSpan.SequenceEqual(utf8NamespaceUri);
        }

        /// <summary>
        /// Compares the text in a read-only character span to the namespace URI of the current element or
        /// attribute and returns a value that indicates whether they match.
        /// </summary>
        /// <param name="namespaceUri">The namespace URI to compare against. An empty span stands for no namespace.</param>
        /// <returns>
        /// <see langword="true"/> if the namespace URI matches <paramref name="namespaceUri"/>; otherwise,
        /// <see langword="false"/>.
        /// </returns>
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
        /// Compares the text in a string to the namespace URI of the current element or attribute and
        /// returns a value that indicates whether they match.
        /// </summary>
        /// <param name="namespaceUri">The namespace URI to compare against. A <see langword="null"/> or empty string stands for no namespace.</param>
        /// <returns>
        /// <see langword="true"/> if the namespace URI matches <paramref name="namespaceUri"/>; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool NamespaceUriEquals(string? namespaceUri)
        {
            return NamespaceUriEquals((namespaceUri ?? "").AsSpan());
        }

        /// <summary>
        /// Reads the next XML token from the input source.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the token was read successfully; otherwise, <see langword="false"/>.
        /// The result is <see langword="false"/> at the end of a complete document, and when more data is
        /// required and <see cref="IsFinalBlock"/> is <see langword="false"/>.
        /// </returns>
        /// <exception cref="XmlException">
        /// The document is not well formed.
        /// -or-
        /// A limit set in <see cref="XmlReaderOptions"/> is exceeded.
        /// -or-
        /// The input ends inside a token and <see cref="IsFinalBlock"/> is <see langword="true"/>.
        /// </exception>
        public bool Read()
        {
            _inAttribute = false;
            _attributeIndex = -1;
            _isEmptyElement = false;

            // The reader leaves the start tag it was on. If this read finds no token, the reader is between
            // tokens, inside every element that is open.
            _isOnOpenStartTag = false;
            _decodedLength = 0;
            _valueIsEscaped = false;

            // A start tag and an end tag have no value. Without this they would report the value of the token
            // read before them.
            _valueLength = 0;
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

            if (_pos == 0 && _documentBytesConsumedBase == 0 && !_isFinalBlock && _span.Length is 1 or 2 && "\uFEFF"u8.StartsWith(_span))
            {
                // The first buffer ends inside what may be a byte order mark. Nothing can be read until the
                // rest of it arrives.
                return NeedMore(rewindToTokenStart: false);
            }

            if (_pos == 0 && _documentBytesConsumedBase == 0 && !_span.IsEmpty && _span[0] is 0xFF or 0xFE or 0x00 or (byte)'<')
            {
                // The first two bytes of UTF-16 and UTF-32 text, with or without a byte order mark, are never
                // the first two bytes of UTF-8 XML. Saying so is more use than the error they lead to.
                if (_span.Length == 1)
                {
                    if (!_isFinalBlock && _span[0] is not (byte)'<')
                    {
                        return NeedMore(rewindToTokenStart: false);
                    }
                }
                else
                {
                    // FF FE and FE FF are byte order marks. "<" beside a zero byte is UTF-16 or UTF-32 with
                    // none, and so are two zero bytes. The last arm is a first byte of zero.
                    var isAnotherEncoding = _span[0] switch
                    {
                        0xFF => _span[1] is 0xFE,
                        0xFE => _span[1] is 0xFF,
                        (byte)'<' => _span[1] is 0x00,
                        _ => _span[1] is (byte)'<' or 0x00,
                    };
                    if (isAnotherEncoding)
                    {
                        throw Error("XmlEssentials is UTF-8 only. The input is UTF-16 or UTF-32.");
                    }
                }
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
        /// Skips the children of the current XML element.
        /// </summary>
        /// <exception cref="XmlException">
        /// The element is not all in the current input and <see cref="IsFinalBlock"/> is <see langword="false"/>.
        /// -or-
        /// The skipped XML is not well formed, exceeds a limit set in <see cref="XmlReaderOptions"/>, or ends
        /// inside a token when <see cref="IsFinalBlock"/> is <see langword="true"/>.
        /// </exception>
        /// <remarks>
        /// When the current token is a start element, the reader moves to its end tag. On an empty element, and
        /// on any token that is not a start element, this method does nothing, as <c>Utf8JsonReader.Skip</c>
        /// does on a value. The next <see cref="Read"/> moves to the following token.
        /// </remarks>
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
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetByte(out byte value)
        {
            return XmlLexical.TryParseByte(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>base64Binary</c>.
        /// </summary>
        /// <param name="value">When this method returns, contains the decoded bytes, or <see langword="null"/> if the value is not valid Base64. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value is valid Base64 and was decoded; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetBytesFromBase64([NotNullWhen(true)] out byte[]? value)
        {
            return XmlLexical.TryDecodeBase64(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>date</c>.
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetDateOnly(out DateOnly value)
        {
            return XmlLexical.TryParseDateOnly(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>dateTime</c>.
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetDateTime(out DateTime value)
        {
            return XmlLexical.TryParseDateTime(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>dateTime</c> with an offset.
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetDateTimeOffset(out DateTimeOffset value)
        {
            return XmlLexical.TryParseDateTimeOffset(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>decimal</c>. Exponent notation is rejected.
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetDecimal(out decimal value)
        {
            return XmlLexical.TryParseDecimal(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>double</c>, including <c>INF</c>, <c>-INF</c>, and <c>NaN</c>.
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetDouble(out double value)
        {
            return XmlLexical.TryParseDouble(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as a GUID.
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetGuid(out Guid value)
        {
            return XmlLexical.TryParseGuid(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as a 16-bit integer (<c>xs:short</c>).
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetInt16(out short value)
        {
            return XmlLexical.TryParseInt16(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as a 32-bit integer (<c>xs:int</c>).
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetInt32(out int value)
        {
            return XmlLexical.TryParseInt32(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as a 64-bit integer (<c>xs:long</c>).
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetInt64(out long value)
        {
            return XmlLexical.TryParseInt64(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as a signed 8-bit integer (<c>xs:byte</c>).
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        [CLSCompliant(false)]
        public readonly bool TryGetSByte(out sbyte value)
        {
            return XmlLexical.TryParseSByte(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>float</c>.
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetSingle(out float value)
        {
            return XmlLexical.TryParseSingle(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>time</c>.
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetTimeOnly(out TimeOnly value)
        {
            return XmlLexical.TryParseTimeOnly(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as XSD <c>duration</c>. Year and month components are rejected.
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetTimeSpan(out TimeSpan value)
        {
            return XmlLexical.TryParseDuration(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as an unsigned 16-bit integer (<c>xs:unsignedShort</c>).
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        [CLSCompliant(false)]
        public readonly bool TryGetUInt16(out ushort value)
        {
            return XmlLexical.TryParseUInt16(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as an unsigned 32-bit integer (<c>xs:unsignedInt</c>).
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        [CLSCompliant(false)]
        public readonly bool TryGetUInt32(out uint value)
        {
            return XmlLexical.TryParseUInt32(ValueSpan, out value);
        }

        /// <summary>
        /// Attempts to read the current value as an unsigned 64-bit integer (<c>xs:unsignedLong</c>).
        /// </summary>
        /// <param name="value">When this method returns, contains the parsed value, if parsing succeeded. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the entire token value can be parsed as the requested type; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        [CLSCompliant(false)]
        public readonly bool TryGetUInt64(out ulong value)
        {
            return XmlLexical.TryParseUInt64(ValueSpan, out value);
        }

        /// <summary>
        /// Tries to skip the children of the current XML element.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if there was enough data for the children to be skipped successfully;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="XmlException">
        /// The skipped XML is not well formed.
        /// -or-
        /// A limit set in <see cref="XmlReaderOptions"/> is exceeded.
        /// -or-
        /// The input ends inside a token and <see cref="IsFinalBlock"/> is <see langword="true"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// When the current token is a start element, the reader moves to its end tag. On an empty element
        /// this method does nothing and returns <see langword="true"/>.
        /// When the element is not all in the current block, the reader is put back on its start tag, as
        /// <c>Utf8JsonReader.TrySkip</c> does, so the caller can supply more data and skip or read it again.
        /// </para>
        /// <para>
        /// When the current token is not a start element, this method does nothing and returns
        /// <see langword="true"/>: the token has no children to skip.
        /// </para>
        /// </remarks>
        public bool TrySkip()
        {
            if (_tokenType != XmlTokenType.StartElement)
            {
                // Only a start tag has children. Any other token is already complete.
                return true;
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
        /// Compares the UTF-8 encoded text in a read-only byte span to the decoded XML token value in the
        /// source and returns a value that indicates whether they match.
        /// </summary>
        /// <param name="utf8Text">The UTF-8 encoded text to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if the XML token value in the source matches the UTF-8 encoded lookup text;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public readonly bool ValueTextEquals(ReadOnlySpan<byte> utf8Text)
        {
            return ValueSpan.SequenceEqual(utf8Text);
        }

        /// <summary>
        /// Compares the text in a read-only character span to the decoded XML token value in the source and
        /// returns a value that indicates whether they match.
        /// </summary>
        /// <param name="text">The text to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if the XML token value in the source matches the lookup text; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool ValueTextEquals(ReadOnlySpan<char> text)
        {
            var max = Encoding.UTF8.GetMaxByteCount(text.Length);
            Span<byte> utf8 = max <= 512 ? stackalloc byte[max] : new byte[max];
            var n = Encoding.UTF8.GetBytes(text, utf8);
            return ValueSpan.SequenceEqual(utf8[..n]);
        }

        /// <summary>
        /// Compares the text in a string to the decoded XML token value in the source and returns a value
        /// that indicates whether they match.
        /// </summary>
        /// <param name="text">The text to compare against. A <see langword="null"/> value matches no token value.</param>
        /// <returns>
        /// <see langword="true"/> if the XML token value in the source matches the lookup text; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool ValueTextEquals(string? text)
        {
            return text is not null && ValueTextEquals(text.AsSpan());
        }

        #endregion

    }

}
