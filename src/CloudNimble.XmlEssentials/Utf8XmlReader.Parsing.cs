using System.Buffers;
using System.Globalization;
using System.Text;

namespace CloudNimble.Text.Xml
{

    public ref partial struct Utf8XmlReader
    {

        #region Private Methods

        /// <summary>
        /// Advances one character, normalizing end-of-line sequences and updating line state.
        /// </summary>
        /// <remarks>
        /// CR, CRLF, and LF become a single line break. In XML 1.1, NEL and LS are line breaks too.
        /// Other characters are validated against the document version's Char production.
        /// </remarks>
        internal void AdvanceChar()
        {
            var b = _span[_pos];
            if (b is (byte)'\r')
            {
                _pos++;
                if (_pos < _span.Length && _span[_pos] is (byte)'\n')
                {
                    _pos++;
                }

                _lineNumber++;
                _lineStart = _pos;
                return;
            }

            if (b is (byte)'\n')
            {
                _pos++;
                _lineNumber++;
                _lineStart = _pos;
                return;
            }

            if (_version is XmlVersion.Version11
                && b is 0xC2
                && _pos + 1 < _span.Length
                && _span[_pos + 1] is 0x85)
            {
                _pos += 2;
                _lineNumber++;
                _lineStart = _pos;
                return;
            }

            if (_version is XmlVersion.Version11
                && b is 0xE2
                && _pos + 2 < _span.Length
                && _span[_pos + 1] is 0x80
                && _span[_pos + 2] is 0xA8)
            {
                _pos += 3;
                _lineNumber++;
                _lineStart = _pos;
                return;
            }

            var (cp, width) = DecodeRune(_pos);
            ValidateChar(cp, fromNcr: false);
            _pos += width;
        }

        /// <summary>
        /// Advances over ASCII text that needs no entity expansion, EOL, or Char check.
        /// </summary>
        /// <param name="length">The maximum number of bytes to inspect from <c>_pos</c>.</param>
        /// <returns><see langword="true"/> if a non-whitespace byte was consumed.</returns>
        internal bool AdvanceAsciiTextRun(int length)
        {
            var run = _span.Slice(_pos, length);
            var bad = run.IndexOfAnyExcept(XmlChars.AsciiTextChars);
            if (bad < 0)
            {
                var hasNonWs = run.IndexOfAnyExcept((byte)' ', (byte)'\t') >= 0;
                _pos += length;
                return hasNonWs;
            }

            if (bad == 0)
            {
                return false;
            }

            var prefix = run[..bad];
            var prefixNonWs = prefix.IndexOfAnyExcept((byte)' ', (byte)'\t') >= 0;
            _pos += bad;
            return prefixNonWs;
        }

        /// <summary>
        /// Restores parse progress from <paramref name="snapshot"/>.
        /// </summary>
        /// <param name="snapshot">The snapshot to apply, or null to leave the reader at the start of a document.</param>
        /// <remarks>
        /// A non-null snapshot means an XML declaration is no longer allowed at this position.
        /// </remarks>
        internal void ApplySnapshot(XmlReaderStateSnapshot? snapshot)
        {
            if (snapshot is null)
            {
                return;
            }

            _depth = snapshot.Depth;
            _documentBytesConsumedBase = snapshot.DocumentBytesConsumed;
            _elementCount = snapshot.ElementLocalNames.Length;
            _resumeNameCount = snapshot.ElementLocalNames.Length;
            _resumeLocalNames = new byte[snapshot.ElementLocalNames.Length][];
            _resumeNsUris = new byte[snapshot.ElementLocalNames.Length][];
            _nsCount = 0;
            _nsBindings = null;
            for (var i = 0; i < snapshot.ElementLocalNames.Length; i++)
            {
                _resumeLocalNames[i] = Encoding.UTF8.GetBytes(snapshot.ElementLocalNames[i]);
                _resumeNsUris[i] = InternUri(Encoding.UTF8.GetBytes(snapshot.ElementNamespaceUris[i]));
            }

            if (snapshot.NamespacePrefixes is { Length: > 0 })
            {
                for (var i = 0; i < snapshot.NamespacePrefixes.Length; i++)
                {
                    PushBinding(
                        snapshot.NamespaceDepths[i],
                        InternPrefix(Encoding.UTF8.GetBytes(snapshot.NamespacePrefixes[i])),
                        InternUri(Encoding.UTF8.GetBytes(snapshot.NamespaceUris[i])));
                }
            }

            SetCurrentNamespace(InternUri(Encoding.UTF8.GetBytes(snapshot.CurrentNamespaceUri)));
            _finished = snapshot.Finished;
            _isEmptyElement = snapshot.IsEmptyElement;
            _lineNumber = snapshot.LineNumber;
            _lineStart = snapshot.LineStart;
            _pendingEmptyNsDepth = snapshot.PendingEmptyNsDepth;
            _seenRoot = snapshot.SeenRoot;
            _tokenType = snapshot.TokenType;
            _version = snapshot.Version;
            _xmlDeclarationAllowed = snapshot.XmlDeclarationAllowed;
        }

        /// <summary>
        /// Copies current parse progress into an immutable snapshot.
        /// </summary>
        /// <returns>A new snapshot. Stacks are copied so the source reader can keep mutating.</returns>
        internal readonly XmlReaderStateSnapshot CaptureSnapshot()
        {
            return new XmlReaderStateSnapshot
            {
                CurrentNamespaceUri = Encoding.UTF8.GetString(CurrentNamespaceBytes()),
                Depth = _depth,
                DocumentBytesConsumed = _documentBytesConsumedBase + _pos,
                ElementLocalNames = MaterializeElementNames(),
                ElementNamespaceUris = MaterializeElementNamespaces(),
                Finished = _finished,
                IsEmptyElement = _isEmptyElement,
                LineNumber = _lineNumber,
                LineStart = _lineStart,
                PendingEmptyNsDepth = _pendingEmptyNsDepth,
                NamespaceDepths = MaterializeNamespaceDepths(),
                NamespacePrefixes = MaterializeNamespacePrefixes(),
                NamespaceUris = MaterializeNamespaceUris(),
                SeenRoot = _seenRoot,
                TokenType = _tokenType,
                Version = _version,
                XmlDeclarationAllowed = _xmlDeclarationAllowed,
            };
        }

        /// <summary>
        /// Returns the attribute at <c>_attributeIndex</c>.
        /// </summary>
        /// <returns>The current attribute record.</returns>
        /// <remarks>
        /// Callers must have selected an attribute with <see cref="MoveToNextAttribute"/>.
        /// </remarks>
        internal readonly AttributeRecord CurrentAttribute()
        {
            if (_attributeIndex < 8)
            {
                return _attributesInline[_attributeIndex];
            }

            return _attributes![_attributeIndex];
        }

        /// <summary>
        /// Decodes <see cref="ValueSpan"/> as UTF-16.
        /// </summary>
        /// <returns>The current token value. Never null; empty when the value is empty.</returns>
        internal readonly string CurrentValueText()
        {
            return Encoding.UTF8.GetString(ValueSpan);
        }

        /// <summary>
        /// Expands entities and normalizes whitespace in an attribute value.
        /// </summary>
        /// <param name="start">Byte offset of the raw value in the current buffer.</param>
        /// <param name="length">UTF-8 byte length of the raw value.</param>
        /// <param name="escaped">True when the result differs from the raw slice.</param>
        /// <param name="decodedLength">Byte length of the returned buffer, or <paramref name="length"/> when no decode ran.</param>
        /// <returns>A decoded buffer, or null when the raw slice is already the value.</returns>
        /// <remarks>
        /// TAB, LF, and CR become SPACE after end-of-line handling, per attribute-value normalization.
        /// </remarks>
        internal byte[]? DecodeAttributeValue(int start, int length, out bool escaped, out int decodedLength)
        {
            escaped = false;
            decodedLength = length;
            var slice = _span.Slice(start, length);
            var xml11Eol = _version is XmlVersion.Version11 && ContainsXml11LineEnding(slice);
            if (slice.IndexOf((byte)'&') < 0
                && slice.IndexOf((byte)'\t') < 0
                && slice.IndexOf((byte)'\n') < 0
                && slice.IndexOf((byte)'\r') < 0
                && !xml11Eol)
            {
                return null;
            }

            var savedPos = _pos;
            var destination = new byte[length];
            var written = 0;
            var i = start;
            Span<byte> entityScratch = stackalloc byte[8];
            while (i < start + length)
            {
                _pos = i;
                if (_span[i] is (byte)'&')
                {
                    if (!DecodeEntity(entityScratch, out var entityWritten))
                    {
                        _pos = savedPos;
                        decodedLength = length;
                        return null;
                    }

                    entityScratch[..entityWritten].CopyTo(destination.AsSpan(written));
                    written += entityWritten;
                    escaped = true;
                    i = _pos;
                    continue;
                }

                if (_span[i] is (byte)'\t' or (byte)'\n' or (byte)'\r')
                {
                    destination[written] = (byte)' ';
                    written++;
                    escaped = true;
                    if (_span[i] is (byte)'\r' && i + 1 < start + length && _span[i + 1] is (byte)'\n')
                    {
                        i += 2;
                        continue;
                    }

                    i++;
                    continue;
                }

                if (_version is XmlVersion.Version11
                    && _span[i] is 0xC2
                    && i + 1 < start + length
                    && _span[i + 1] is 0x85)
                {
                    destination[written] = (byte)' ';
                    written++;
                    escaped = true;
                    i += 2;
                    continue;
                }

                if (_version is XmlVersion.Version11
                    && _span[i] is 0xE2
                    && i + 2 < start + length
                    && _span[i + 1] is 0x80
                    && _span[i + 2] is 0xA8)
                {
                    destination[written] = (byte)' ';
                    written++;
                    escaped = true;
                    i += 3;
                    continue;
                }

                destination[written] = _span[i];
                written++;
                i++;
            }

            _pos = savedPos;
            escaped = true;
            decodedLength = written;
            return destination;
        }

        /// <summary>
        /// Consumes a predefined entity or numeric character reference starting at the current <c>&amp;</c>.
        /// </summary>
        /// <param name="destination">Buffer that receives the UTF-8 replacement. Must hold at least 4 bytes for a numeric reference.</param>
        /// <param name="written">When this method returns, the number of bytes written.</param>
        /// <returns><see langword="true"/> if a complete reference was consumed.</returns>
        /// <exception cref="XmlException">The reference is undefined, not a legal Char, or not a RestrictedChar NCR in 1.1.</exception>
        /// <remarks>
        /// Only <c>lt</c>, <c>gt</c>, <c>amp</c>, <c>quot</c>, <c>apos</c>, and numeric references are accepted.
        /// Returns false when the buffer ends inside the reference and more data may still arrive.
        /// </remarks>
        internal bool DecodeEntity(scoped Span<byte> destination, out int written)
        {
            written = 0;
            var start = _pos;
            _pos++;
            if (_pos >= _span.Length)
            {
                _pos = start;
                return false;
            }

            if (_span[_pos] is (byte)'#')
            {
                _pos++;
                var hex = _pos < _span.Length && _span[_pos] is (byte)'x' or (byte)'X';
                if (hex)
                {
                    _pos++;
                }

                var numberStart = _pos;
                while (_pos < _span.Length && IsHexOrDec(_span[_pos], hex))
                {
                    _pos++;
                }

                if (_pos >= _span.Length || _span[_pos] is not (byte)';')
                {
                    _pos = start;
                    return false;
                }

                if (!TryParseCodePoint(_span.Slice(numberStart, _pos - numberStart), hex, out var cp)
                    || cp < 0
                    || cp > 0x10FFFF)
                {
                    throw Error("Invalid character reference.");
                }

                _pos++;
                ValidateChar(cp, fromNcr: true);
                written = EncodeUtf8CodePoint(cp, destination);
                return true;
            }

            var nameStart = _pos;
            while (_pos < _span.Length && _span[_pos] is not (byte)';')
            {
                _pos++;
            }

            if (_pos >= _span.Length)
            {
                _pos = start;
                return false;
            }

            var name = _span.Slice(nameStart, _pos - nameStart);
            _pos++;
            byte replacement;
            if (name.SequenceEqual("amp"u8))
            {
                replacement = (byte)'&';
            }
            else if (name.SequenceEqual("apos"u8))
            {
                replacement = (byte)'\'';
            }
            else if (name.SequenceEqual("gt"u8))
            {
                replacement = (byte)'>';
            }
            else if (name.SequenceEqual("lt"u8))
            {
                replacement = (byte)'<';
            }
            else if (name.SequenceEqual("quot"u8))
            {
                replacement = (byte)'"';
            }
            else
            {
                throw Error("Undefined entity.");
            }

            if (destination.Length < 1)
            {
                throw Error("Entity destination is too small.");
            }

            destination[0] = replacement;
            written = 1;
            return true;
        }

        /// <summary>
        /// Decodes one Unicode code point at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">Byte offset in the current buffer.</param>
        /// <returns>The code point and the number of UTF-8 bytes it occupies.</returns>
        /// <exception cref="XmlException">The bytes are not valid UTF-8.</exception>
        internal (int CodePoint, int Width) DecodeRune(int index)
        {
            var span = _span.Slice(index);
            if (!span.IsEmpty && span[0] < 0x80)
            {
                return (span[0], 1);
            }

            var status = Rune.DecodeFromUtf8(span, out var rune, out var consumed);
            if (status is not OperationStatus.Done)
            {
                throw Error("Invalid UTF-8.");
            }

            return (rune.Value, consumed);
        }

        /// <summary>
        /// Expands entities in a text node into <c>_decodedValue</c> when the slice contains <c>&amp;</c>.
        /// </summary>
        /// <param name="start">Byte offset of the raw text in the current buffer.</param>
        /// <param name="length">UTF-8 byte length of the raw text.</param>
        internal void DecodeValueSpan(int start, int length)
        {
            var slice = _span.Slice(start, length);
            var needsEol = slice.IndexOf((byte)'\r') >= 0 || (_version is XmlVersion.Version11 && ContainsXml11LineEnding(slice));
            if (slice.IndexOf((byte)'&') < 0 && !needsEol)
            {
                _valueIsEscaped = false;
                return;
            }

            EnsureDecodedCapacity(length);
            var written = 0;
            var i = 0;
            Span<byte> entityScratch = stackalloc byte[8];
            while (i < slice.Length)
            {
                if (slice[i] is (byte)'&')
                {
                    var saved = _pos;
                    _pos = start + i;
                    if (!DecodeEntity(entityScratch, out var entityWritten))
                    {
                        _pos = saved;
                        break;
                    }

                    entityScratch[..entityWritten].CopyTo(_decodedValue.AsSpan(written));
                    written += entityWritten;
                    i = _pos - start;
                    _pos = saved;
                    continue;
                }

                if (slice[i] is (byte)'\r')
                {
                    _decodedValue![written] = (byte)'\n';
                    written++;
                    i++;
                    if (i < slice.Length && slice[i] is (byte)'\n')
                    {
                        i++;
                    }

                    continue;
                }

                if (_version is XmlVersion.Version11 && slice[i] is 0xC2 && i + 1 < slice.Length && slice[i + 1] is 0x85)
                {
                    _decodedValue![written] = (byte)'\n';
                    written++;
                    i += 2;
                    continue;
                }

                if (_version is XmlVersion.Version11 && slice[i] is 0xE2 && i + 2 < slice.Length && slice[i + 1] is 0x80 && slice[i + 2] is 0xA8)
                {
                    _decodedValue![written] = (byte)'\n';
                    written++;
                    i += 3;
                    continue;
                }

                _decodedValue![written] = slice[i];
                written++;
                i++;
            }

            _decodedLength = written;
            _valueIsEscaped = true;
        }

        /// <summary>
        /// Returns true if <paramref name="slice"/> contains a UTF-8 NEL or LS.
        /// </summary>
        /// <param name="slice">The text bytes.</param>
        /// <returns><see langword="true"/> if an XML 1.1 extra line ending is present.</returns>
        internal static bool ContainsXml11LineEnding(ReadOnlySpan<byte> slice)
        {
            for (var i = 0; i < slice.Length; i++)
            {
                if (slice[i] is 0xC2 && i + 1 < slice.Length && slice[i + 1] is 0x85)
                {
                    return true;
                }

                if (slice[i] is 0xE2 && i + 2 < slice.Length && slice[i + 1] is 0x80 && slice[i + 2] is 0xA8)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the local name of the in-progress element at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The element-stack index.</param>
        /// <returns>UTF-8 local name bytes from the current buffer or a resume copy.</returns>
        internal readonly ReadOnlySpan<byte> ElementLocalName(int index)
        {
            if (index < _resumeNameCount && _resumeLocalNames is not null)
            {
                return _resumeLocalNames[index];
            }

            if (index < 8)
            {
                return _span.Slice(_elementNameStartsInline[index], _elementNameLengthsInline[index]);
            }

            return _span.Slice(_elementNameStarts![index], _elementNameLengths![index]);
        }

        /// <summary>
        /// Encodes <paramref name="cp"/> as UTF-8 into <paramref name="destination"/>.
        /// </summary>
        /// <param name="cp">The Unicode code point.</param>
        /// <param name="destination">The destination buffer. Must hold up to 4 bytes.</param>
        /// <returns>The number of bytes written.</returns>
        internal static int EncodeUtf8CodePoint(int cp, scoped Span<byte> destination)
        {
            if (cp <= 0x7F)
            {
                destination[0] = (byte)cp;
                return 1;
            }

            if (cp <= 0x7FF)
            {
                destination[0] = (byte)(0xC0 | (cp >> 6));
                destination[1] = (byte)(0x80 | (cp & 0x3F));
                return 2;
            }

            if (cp <= 0xFFFF)
            {
                destination[0] = (byte)(0xE0 | (cp >> 12));
                destination[1] = (byte)(0x80 | ((cp >> 6) & 0x3F));
                destination[2] = (byte)(0x80 | (cp & 0x3F));
                return 3;
            }

            destination[0] = (byte)(0xF0 | (cp >> 18));
            destination[1] = (byte)(0x80 | ((cp >> 12) & 0x3F));
            destination[2] = (byte)(0x80 | ((cp >> 6) & 0x3F));
            destination[3] = (byte)(0x80 | (cp & 0x3F));
            return 4;
        }

        /// <summary>
        /// Grows <c>_decodedValue</c> so it can hold <paramref name="needed"/> bytes.
        /// </summary>
        /// <param name="needed">The required capacity.</param>
        internal void EnsureDecodedCapacity(int needed)
        {
            if (_decodedValue is not null && _decodedValue.Length >= needed)
            {
                return;
            }

            _decodedValue = new byte[needed];
        }

        /// <summary>
        /// Grows the element-name slice stacks.
        /// </summary>
        internal void EnsureElementCapacity()
        {
            if (_elementCount < 8)
            {
                return;
            }

            if (_elementNameStarts is not null && _elementCount < _elementNameStarts.Length)
            {
                return;
            }

            var n = _elementNameStarts is null ? 16 : _elementNameStarts.Length * 2;
            Array.Resize(ref _elementNameStarts, n);
            Array.Resize(ref _elementNameLengths, n);
            if (n == 16)
            {
                for (var i = 0; i < 8; i++)
                {
                    _elementNameStarts[i] = _elementNameStartsInline[i];
                    _elementNameLengths[i] = _elementNameLengthsInline[i];
                }
            }
        }

        /// <summary>
        /// Creates an <see cref="XmlException"/> at the current byte offset and line.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The exception. Callers throw it.</returns>
        internal readonly XmlException Error(string message)
        {
            var linePosition = _pos - _lineStart + 1;
            return new XmlException(message, _documentBytesConsumedBase + _pos, _lineNumber, linePosition, path: null);
        }

        /// <summary>
        /// Applies <see cref="XmlReaderOptions.WhitespaceHandling"/> to a whitespace-only text node.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the node is reported as <see cref="XmlTokenType.Text"/>;
        /// <see langword="false"/> when it is skipped.
        /// </returns>
        /// <exception cref="XmlException">Handling is <see cref="XmlWhitespaceHandling.None"/>.</exception>
        internal bool HandleWhitespaceOnlyText()
        {
            if (_options.WhitespaceHandling is XmlWhitespaceHandling.Significant)
            {
                _tokenType = XmlTokenType.Text;
                return true;
            }

            if (_options.WhitespaceHandling is XmlWhitespaceHandling.None)
            {
                throw Error("Whitespace-only text is not allowed.");
            }

            return false;
        }

        /// <summary>
        /// Returns true if <paramref name="b"/> is a decimal digit, or a hex digit when <paramref name="hex"/> is true.
        /// </summary>
        /// <param name="b">The byte to test.</param>
        /// <param name="hex">True to accept <c>a-f</c> and <c>A-F</c>.</param>
        /// <returns><see langword="true"/> if the byte is allowed in the character reference number.</returns>
        internal static bool IsHexOrDec(byte b, bool hex)
        {
            if (b is >= (byte)'0' and <= (byte)'9')
            {
                return true;
            }

            return hex && b is (>= (byte)'a' and <= (byte)'f') or (>= (byte)'A' and <= (byte)'F');
        }

        /// <summary>
        /// Copies in-progress element local names to independent strings for a snapshot.
        /// </summary>
        /// <returns>One string per open element.</returns>
        internal readonly string[] MaterializeElementNames()
        {
            var names = new string[_elementCount];
            for (var i = 0; i < _elementCount; i++)
            {
                names[i] = Encoding.UTF8.GetString(ElementLocalName(i));
            }

            return names;
        }

        /// <summary>
        /// Marks the current token incomplete.
        /// </summary>
        /// <returns><see langword="false"/> when more data may still arrive.</returns>
        /// <exception cref="XmlException"><see cref="IsFinalBlock"/> is true, so the document ended inside a token.</exception>
        internal bool NeedMore()
        {
            return NeedMore(rewindToTokenStart: true);
        }

        /// <summary>
        /// Marks the current token incomplete, optionally rewinding to the token start so the next buffer can reparse it.
        /// </summary>
        /// <param name="rewindToTokenStart">True when the current token is incomplete and must be reread from its first byte.</param>
        /// <returns><see langword="false"/> when more data may still arrive.</returns>
        /// <exception cref="XmlException"><see cref="IsFinalBlock"/> is true, so the document ended inside a token.</exception>
        internal bool NeedMore(bool rewindToTokenStart)
        {
            _incomplete = true;
            if (_isFinalBlock)
            {
                throw Error("Unexpected end of XML.");
            }

            if (rewindToTokenStart)
            {
                var offset = (int)(_tokenStartIndex - _documentBytesConsumedBase);
                if (offset >= 0 && offset <= _pos)
                {
                    _pos = offset;
                    _lineNumber = _tokenLineNumber;
                    _lineStart = _tokenLineStart;
                }
            }

            return false;
        }

        /// <summary>
        /// Parses the current value as an XSD integer using <paramref name="parse"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The integer type.
        /// </typeparam>
        /// <param name="parse">The framework parse function.</param>
        /// <returns>The parsed value.</returns>
        /// <exception cref="XmlException">The lexical form uses <c>+</c>, exponent, grouping, or overflows <typeparamref name="T"/>.</exception>
        internal T ParseInteger<T>(Func<string, NumberStyles, IFormatProvider, T> parse)
            where T : struct
        {
            var s = TrimAscii(CurrentValueText());
            if (s.StartsWith('+') || s.Contains('e', StringComparison.OrdinalIgnoreCase) || s.Contains('_') || s.Contains(','))
            {
                throw Error("Invalid integer.");
            }

            try
            {
                return parse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw Error("Invalid integer.");
            }
        }

        /// <summary>
        /// Reads <c>version</c> and <c>encoding</c> from the current XML declaration token.
        /// </summary>
        /// <exception cref="XmlException">The declared encoding is not UTF-8.</exception>
        /// <remarks>
        /// An omitted version means <see cref="XmlVersion.Version10"/>. The <c>standalone</c> pseudo-attribute is ignored.
        /// </remarks>
        internal void ParseXmlDeclaration()
        {
            var text = _span.Slice(_valueStart, _valueLength);
            Span<byte> folded = text.Length <= 256 ? stackalloc byte[text.Length] : new byte[text.Length];
            text.CopyTo(folded);
            for (var i = 0; i < folded.Length; i++)
            {
                if (folded[i] is >= (byte)'A' and <= (byte)'Z')
                {
                    folded[i] = (byte)(folded[i] | 0x20);
                }
            }
            if (folded.IndexOf("version"u8) >= 0)
            {
                _version = folded.IndexOf("1.1"u8) >= 0
                    ? XmlVersion.Version11
                    : XmlVersion.Version10;
            }

            var encIndex = folded.IndexOf("encoding="u8);
            if (encIndex < 0)
            {
                return;
            }

            var rest = TrimXmlWhitespace(folded[(encIndex + "encoding=".Length)..]);
            if (rest.Length < 2)
            {
                return;
            }

            var q = rest[0];
            var end = rest[1..].IndexOf(q);
            if (end <= 0)
            {
                return;
            }

            var enc = rest.Slice(1, end);
            if (!enc.SequenceEqual("utf-8"u8) && !enc.SequenceEqual("utf8"u8))
            {
                throw Error($"XmlEssentials is UTF-8 only. Declared encoding '{Encoding.UTF8.GetString(enc)}' is not supported.");
            }
        }

        /// <summary>
        /// Reads one attribute from the current start tag.
        /// </summary>
        /// <returns><see langword="true"/> if the attribute was stored; <see langword="false"/> if more data is required.</returns>
        /// <exception cref="XmlException">The attribute is malformed, duplicated, or exceeds <see cref="XmlReaderOptions.MaxAttributeCount"/>.</exception>
        internal bool ReadAttribute()
        {
            if (_attributeCount >= _maxAttributes)
            {
                throw Error($"{nameof(XmlReaderOptions.MaxAttributeCount)} exceeded ({_maxAttributes}).");
            }

            var nameStart = _pos;
            if (!ReadName(out var prefixLength, out var nameLength))
            {
                if (_incomplete)
                {
                    return false;
                }

                throw Error("Invalid attribute name.");
            }

            SkipWhitespaceBytes();
            if (_pos >= _span.Length)
            {
                return NeedMore();
            }

            if (_span[_pos] is not (byte)'=')
            {
                throw Error("Expected '=' in attribute.");
            }

            _pos++;
            SkipWhitespaceBytes();
            if (_pos >= _span.Length)
            {
                return NeedMore();
            }

            var quote = _span[_pos];
            if (quote is not (byte)'"' and not (byte)'\'')
            {
                throw Error("Expected attribute quote.");
            }

            if (nameLength >= 5
                && (prefixLength == 5 || (prefixLength == 0 && nameLength == 5))
                && _span.Slice(nameStart, 5).SequenceEqual("xmlns"u8))
            {
                _hasNamespaceAttribute = true;
            }

            _pos++;
            var valueStart = _pos;
            var stops = quote is (byte)'"' ? XmlChars.AttrStopsDouble : XmlChars.AttrStopsSingle;
            Span<byte> entityScratch = stackalloc byte[8];
            while (_pos < _span.Length && _span[_pos] != quote)
            {
                var rest = _span[_pos..];
                var n = rest.IndexOfAny(stops);
                if (n < 0)
                {
                    return NeedMore();
                }

                if (n > 0)
                {
                    _ = AdvanceAsciiTextRun(n);
                }

                if (_pos >= _span.Length || _span[_pos] == quote)
                {
                    break;
                }

                if (_span[_pos] is (byte)'<')
                {
                    throw Error("'<' is not allowed in attribute values.");
                }

                if (_span[_pos] is (byte)'&')
                {
                    if (!DecodeEntity(entityScratch, out _))
                    {
                        return NeedMore();
                    }

                    continue;
                }

                AdvanceChar();
            }

            if (_pos >= _span.Length)
            {
                return NeedMore();
            }

            var valueLength = _pos - valueStart;
            _pos++;
            var record = new AttributeRecord
            {
                Decoded = DecodeAttributeValue(valueStart, valueLength, out var escaped, out var decodedLength),
                DecodedLength = decodedLength,
                NameLength = nameLength,
                NameStart = nameStart,
                PrefixLength = prefixLength,
                ValueIsEscaped = escaped,
                ValueLength = valueLength,
                ValueStart = valueStart,
            };
            if (_attributeCount < 8)
            {
                _attributesInline[_attributeCount] = record;
            }
            else
            {
                if (_attributes is null)
                {
                    _attributes = new AttributeRecord[16];
                    for (var i = 0; i < 8; i++)
                    {
                        _attributes[i] = _attributesInline[i];
                    }
                }
                else if (_attributeCount == _attributes.Length)
                {
                    Array.Resize(ref _attributes, _attributes.Length * 2);
                }

                _attributes[_attributeCount] = record;
            }

            _attributeCount++;
            return true;
        }

        /// <summary>
        /// Reads a CDATA section starting at <c>&lt;![CDATA[</c>.
        /// </summary>
        /// <returns><see langword="true"/> if the section was completed; <see langword="false"/> if more data is required.</returns>
        /// <exception cref="XmlException">The section exceeds <see cref="XmlReaderOptions.MaxTextByteCount"/>, or the buffer ends and <see cref="IsFinalBlock"/> is true.</exception>
        internal bool ReadCData()
        {
            _pos += 9;
            var start = _pos;
            while (_pos + 2 < _span.Length)
            {
                if (_span[_pos] is (byte)']' && _span[_pos + 1] is (byte)']' && _span[_pos + 2] is (byte)'>')
                {
                    _valueStart = start;
                    _valueLength = _pos - start;
                    _pos += 3;
                    if (_valueLength > _maxTextBytes)
                    {
                        throw Error($"{nameof(XmlReaderOptions.MaxTextByteCount)} exceeded ({_maxTextBytes}).");
                    }

                    _tokenType = XmlTokenType.CData;
                    return true;
                }

                AdvanceChar();
            }

            return NeedMore();
        }

        /// <summary>
        /// Reads a comment starting at <c>&lt;!--</c>.
        /// </summary>
        /// <returns><see langword="true"/> if the comment was completed; <see langword="false"/> if more data is required.</returns>
        /// <exception cref="XmlException">The comment contains <c>--</c>, exceeds <see cref="XmlReaderOptions.MaxCommentByteCount"/>, or is truncated on a final buffer.</exception>
        internal bool ReadComment()
        {
            _pos += 4;
            var start = _pos;
            while (_pos + 1 < _span.Length)
            {
                if (_span[_pos] is (byte)'-' && _span[_pos + 1] is (byte)'-')
                {
                    if (_pos + 2 >= _span.Length)
                    {
                        return NeedMore();
                    }

                    if (_span[_pos + 2] is not (byte)'>')
                    {
                        throw Error("Comments must not contain '--'.");
                    }

                    _valueStart = start;
                    _valueLength = _pos - start;
                    _pos += 3;
                    if (_valueLength > _maxCommentBytes)
                    {
                        throw Error($"{nameof(XmlReaderOptions.MaxCommentByteCount)} exceeded ({_maxCommentBytes}).");
                    }

                    _tokenType = XmlTokenType.Comment;
                    return true;
                }

                AdvanceChar();
            }

            return NeedMore();
        }

        /// <summary>
        /// Reads markup inside an element: child tags, text, comments, and PIs.
        /// </summary>
        /// <returns><see langword="true"/> if a token was produced; <see langword="false"/> if more data is required.</returns>
        internal bool ReadElementContent()
        {
            while (true)
            {
                if (_pos >= _span.Length)
                {
                    if (!_isFinalBlock)
                    {
                        return NeedMore(rewindToTokenStart: false);
                    }

                    throw Error("Unexpected end of XML.");
                }

                _tokenStartIndex = _documentBytesConsumedBase + _pos;
                _tokenLineNumber = _lineNumber;
                _tokenLineStart = _lineStart;
                if (_span[_pos] is not (byte)'<')
                {
                    if (ReadText())
                    {
                        return true;
                    }

                    if (_incomplete)
                    {
                        return false;
                    }

                    continue;
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
                            continue;
                        }

                        if (_options.CommentHandling is XmlCommentHandling.Disallow)
                        {
                            throw Error("Comments are not allowed.");
                        }
                    }

                    return true;
                }

                if (next is (byte)'?')
                {
                    if (!ReadPiOrDeclaration())
                    {
                        return false;
                    }

                    return true;
                }

                return ReadStartElement();
            }
        }

        /// <summary>
        /// Reads an end tag starting at <c>&lt;/</c>.
        /// </summary>
        /// <returns><see langword="true"/> if the tag was completed; <see langword="false"/> if more data is required.</returns>
        /// <exception cref="XmlException">The tag is malformed or does not match the current start tag's local name.</exception>
        internal bool ReadEndElement()
        {
            _pos += 2;
            int nameStart;
            int nameLength;
            int prefixLength;
            if (!(_elementCount > 0 && TryMatchEndName(out nameStart, out nameLength, out prefixLength)))
            {
                nameStart = _pos;
                if (!ReadName(out prefixLength, out nameLength))
                {
                    if (_incomplete)
                    {
                        return false;
                    }

                    throw Error("Invalid end tag.");
                }
            }

            SkipWhitespaceBytes();
            if (_pos >= _span.Length)
            {
                return NeedMore();
            }

            if (_span[_pos] is not (byte)'>')
            {
                throw Error("Expected '>' in end tag.");
            }

            _pos++;
            var localAdj = prefixLength > 0 ? prefixLength + 1 : 0;
            var local = _span.Slice(nameStart + localAdj, nameLength - localAdj);
            var prefix = prefixLength > 0 ? _span.Slice(nameStart, prefixLength) : [];
            var endNs = ResolveElementNamespace(prefix);
            if (_elementCount == 0
                || !ElementLocalName(_elementCount - 1).SequenceEqual(local)
                || !ElementNamespace(_elementCount - 1).SequenceEqual(endNs))
            {
                throw Error("End tag does not match start tag.");
            }

            SetCurrentNamespace(endNs);
            PopNamespaceScope(_depth);
            _elementCount--;
            _depth--;
            _nameStart = nameStart;
            _nameLength = nameLength;
            _prefixLength = prefixLength;
            _tokenType = XmlTokenType.EndElement;
            _attributeCount = 0;
            return true;
        }

        /// <summary>
        /// Reads an XML Name at the current position, allowing a single colon.
        /// </summary>
        /// <param name="prefixLength">UTF-8 byte length of the prefix, or 0 when unprefixed.</param>
        /// <param name="nameLength">UTF-8 byte length of the whole name, including the colon.</param>
        /// <returns><see langword="true"/> if a name was read.</returns>
        /// <exception cref="XmlException">The name has multiple colons, an invalid local part, or exceeds <see cref="XmlReaderOptions.MaxNameByteCount"/>.</exception>
        internal bool ReadName(out int prefixLength, out int nameLength)
        {
            prefixLength = 0;
            nameLength = 0;
            var start = _pos;
            if (_pos >= _span.Length)
            {
                return NeedMore();
            }

            var b = _span[_pos];
            if (b < 0x80)
            {
                if (!XmlChars.IsAsciiNameStart(b))
                {
                    return false;
                }

                _pos++;
            }
            else
            {
                var (cp, width) = DecodeRune(_pos);
                if (!XmlChars.IsNameStartChar(cp, _version))
                {
                    return false;
                }

                _pos += width;
            }

            while (_pos < _span.Length)
            {
                var rest = _span[_pos..];
                var n = rest.IndexOfAnyExcept(XmlChars.AsciiNcNameChars);
                if (n < 0)
                {
                    _pos = _span.Length;
                    break;
                }

                _pos += n;
                b = _span[_pos];
                if (b is (byte)':')
                {
                    if (prefixLength > 0)
                    {
                        throw Error("Multiple colons in name.");
                    }

                    prefixLength = _pos - start;
                    _pos++;
                    if (_pos >= _span.Length)
                    {
                        return NeedMore();
                    }

                    b = _span[_pos];
                    if (b < 0x80)
                    {
                        if (b is (byte)':')
                        {
                            throw Error("Multiple colons in name.");
                        }

                        if (!XmlChars.IsAsciiNameStart(b))
                        {
                            throw Error("Invalid name.");
                        }

                        _pos++;
                        continue;
                    }

                    var (cp, width) = DecodeRune(_pos);
                    if (!XmlChars.IsNameStartChar(cp, _version))
                    {
                        throw Error("Invalid name.");
                    }

                    _pos += width;
                    continue;
                }

                if (b < 0x80)
                {
                    break;
                }

                var (cp2, width2) = DecodeRune(_pos);
                if (!XmlChars.IsNameChar(cp2, _version))
                {
                    break;
                }

                _pos += width2;
            }

            nameLength = _pos - start;
            if (nameLength > _maxNameBytes)
            {
                throw Error($"{nameof(XmlReaderOptions.MaxNameByteCount)} exceeded ({_maxNameBytes}).");
            }

            return nameLength > 0;
        }

        /// <summary>
        /// Reads a processing instruction or the XML declaration starting at <c>&lt;?</c>.
        /// </summary>
        /// <returns><see langword="true"/> if the token was completed; <see langword="false"/> if more data is required.</returns>
        /// <exception cref="XmlException">The PI is malformed, or an XML declaration appears after the first token.</exception>
        /// <remarks>
        /// A target of <c>xml</c> (any case) is the XML declaration and is legal only at the start of the document.
        /// </remarks>
        internal bool ReadPiOrDeclaration()
        {
            _pos += 2;
            if (!ReadName(out _, out var targetLength))
            {
                if (_incomplete)
                {
                    return false;
                }

                throw Error("Invalid processing instruction.");
            }

            var targetStart = _pos - targetLength;
            var target = _span.Slice(targetStart, targetLength);
            SkipWhitespaceBytes();
            var valueStart = _pos;
            while (_pos + 1 < _span.Length)
            {
                if (_span[_pos] is (byte)'?' && _span[_pos + 1] is (byte)'>')
                {
                    _valueStart = valueStart;
                    _valueLength = _pos - valueStart;
                    _pos += 2;
                    if (target.Length == 3
                        && target[0] is (byte)'x' or (byte)'X'
                        && target[1] is (byte)'m' or (byte)'M'
                        && target[2] is (byte)'l' or (byte)'L')
                    {
                        if (!_xmlDeclarationAllowed)
                        {
                            throw Error("The XML declaration must be the first token in the document.");
                        }

                        ParseXmlDeclaration();
                        _tokenType = XmlTokenType.XmlDeclaration;
                        _nameStart = targetStart;
                        _nameLength = targetLength;
                        _prefixLength = 0;
                        _xmlDeclarationAllowed = false;
                        return true;
                    }

                    _xmlDeclarationAllowed = false;
                    _tokenType = XmlTokenType.ProcessingInstruction;
                    _nameStart = targetStart;
                    _nameLength = targetLength;
                    _prefixLength = 0;
                    return true;
                }

                AdvanceChar();
            }

            return NeedMore();
        }

        /// <summary>
        /// Reads a start tag, including empty elements.
        /// </summary>
        /// <returns><see langword="true"/> if the tag was completed; <see langword="false"/> if more data is required.</returns>
        /// <exception cref="XmlException">The tag is malformed, a second root is forbidden, or <see cref="XmlReaderOptions.MaxDepth"/> is exceeded.</exception>
        /// <remarks>
        /// Empty elements are reported as <see cref="XmlTokenType.StartElement"/> with <see cref="IsEmptyElement"/> true.
        /// They do not push the element stack.
        /// </remarks>
        internal bool ReadStartElement()
        {
            _pos++;
            var nameStart = _pos;
            if (!ReadName(out var prefixLength, out var nameLength))
            {
                if (_incomplete)
                {
                    return false;
                }

                throw Error("Invalid start tag.");
            }

            _nameStart = nameStart;
            _nameLength = nameLength;
            _prefixLength = prefixLength;
            _attributeCount = 0;
            _hasNamespaceAttribute = false;
            _isEmptyElement = false;

            while (true)
            {
                SkipWhitespaceBytes();
                if (_pos >= _span.Length)
                {
                    return NeedMore();
                }

                var b = _span[_pos];
                if (b is (byte)'>')
                {
                    _pos++;
                    break;
                }

                if (b is (byte)'/')
                {
                    if (_pos + 1 >= _span.Length)
                    {
                        return NeedMore();
                    }

                    if (_span[_pos + 1] is not (byte)'>')
                    {
                        throw Error("Expected '/>'.");
                    }

                    _pos += 2;
                    _isEmptyElement = true;
                    break;
                }

                if (!ReadAttribute())
                {
                    return false;
                }
            }

            if (_seenRoot && _depth == 0 && !_options.AllowMultipleRoots)
            {
                throw Error("Multiple root elements are not allowed.");
            }

            if (_depth + 1 > _maxDepth)
            {
                throw Error($"{nameof(XmlReaderOptions.MaxDepth)} exceeded ({_maxDepth}).");
            }

            ApplyNamespaces(_depth + 1);
            CheckDuplicateNamespacedAttributes();

            if (!_isEmptyElement)
            {
                var localAdj = prefixLength > 0 ? prefixLength + 1 : 0;
                EnsureElementCapacity();
                if (_elementCount < 8)
                {
                    _elementNameStartsInline[_elementCount] = nameStart + localAdj;
                    _elementNameLengthsInline[_elementCount] = nameLength - localAdj;
                }
                else
                {
                    _elementNameStarts![_elementCount] = nameStart + localAdj;
                    _elementNameLengths![_elementCount] = nameLength - localAdj;
                }

                _elementCount++;
                _depth++;
            }
            else if (_hasNamespaceAttribute)
            {
                _pendingEmptyNsDepth = _depth + 1;
            }

            _seenRoot = true;
            _xmlDeclarationAllowed = false;
            _tokenType = XmlTokenType.StartElement;
            return true;
        }

        /// <summary>
        /// Reads character data up to the next <c>&lt;</c>.
        /// </summary>
        /// <returns><see langword="true"/> if a text token was produced; <see langword="false"/> if it was skipped or more data is required.</returns>
        /// <exception cref="XmlException">An entity is illegal, or the node exceeds <see cref="XmlReaderOptions.MaxTextByteCount"/>.</exception>
        internal bool ReadText()
        {
            var start = _pos;
            var hasNonWs = false;
            var stops = _version is XmlVersion.Version11 ? XmlChars.TextStops11 : XmlChars.TextStops10;
            Span<byte> entityScratch = stackalloc byte[8];
            while (_pos < _span.Length && _span[_pos] is not (byte)'<')
            {
                var rest = _span[_pos..];
                var n = rest.IndexOfAny(stops);
                if (n < 0)
                {
                    if (!_isFinalBlock)
                    {
                        return NeedMore();
                    }

                    n = rest.Length;
                    if (n > 0)
                    {
                        hasNonWs |= AdvanceAsciiTextRun(n);
                    }

                    while (_pos < _span.Length)
                    {
                        if (!XmlChars.IsWhiteSpace(_span[_pos]))
                        {
                            hasNonWs = true;
                        }

                        AdvanceChar();
                    }

                    break;
                }

                if (n > 0)
                {
                    hasNonWs |= AdvanceAsciiTextRun(n);
                }

                if (_pos >= _span.Length || _span[_pos] is (byte)'<')
                {
                    break;
                }

                if (_span[_pos] is (byte)'&')
                {
                    hasNonWs = true;
                    if (!DecodeEntity(entityScratch, out _))
                    {
                        return NeedMore();
                    }

                    continue;
                }

                if (!XmlChars.IsWhiteSpace(_span[_pos]))
                {
                    hasNonWs = true;
                }

                AdvanceChar();
            }

            if (_pos >= _span.Length && !_isFinalBlock)
            {
                return NeedMore();
            }

            _valueStart = start;
            _valueLength = _pos - start;
            if (_valueLength > _maxTextBytes)
            {
                throw Error($"{nameof(XmlReaderOptions.MaxTextByteCount)} exceeded ({_maxTextBytes}).");
            }

            DecodeValueSpan(start, _valueLength);
            if (!hasNonWs)
            {
                return HandleWhitespaceOnlyText();
            }

            _tokenType = XmlTokenType.Text;
            return true;
        }

        /// <summary>
        /// Consumes a UTF-8 BOM at offset 0 when present.
        /// </summary>
        internal void SkipBomIfAtStart()
        {
            if (_span.Length >= 3 && _span[0] is 0xEF && _span[1] is 0xBB && _span[2] is 0xBF)
            {
                _pos = 3;
                _lineStart = 3;
            }
        }

        /// <summary>
        /// Advances past space, tab, LF, and CR.
        /// </summary>
        internal void SkipWhitespaceBytes()
        {
            while (_pos < _span.Length && XmlChars.IsWhiteSpace(_span[_pos]))
            {
                AdvanceChar();
            }
        }

        /// <summary>
        /// Trims leading and trailing whitespace from a decoded token value.
        /// </summary>
        /// <param name="value">The decoded text.</param>
        /// <returns>The trimmed string.</returns>
        internal static string TrimAscii(string value)
        {
            return value.Trim();
        }

        /// <summary>
        /// Trims XML whitespace from both ends of a UTF-8 span.
        /// </summary>
        /// <param name="span">The span to trim.</param>
        /// <returns>The trimmed span.</returns>
        internal static ReadOnlySpan<byte> TrimXmlWhitespace(ReadOnlySpan<byte> span)
        {
            while (span.Length > 0 && XmlChars.IsWhiteSpace(span[0]))
            {
                span = span[1..];
            }

            while (span.Length > 0 && XmlChars.IsWhiteSpace(span[^1]))
            {
                span = span[..^1];
            }

            return span;
        }

        /// <summary>
        /// Matches an end tag against the current element's local name without a full name scan.
        /// </summary>
        /// <param name="nameStart">When this method returns true, the byte offset of the qualified name.</param>
        /// <param name="nameLength">When this method returns true, the qualified-name byte length.</param>
        /// <param name="prefixLength">When this method returns true, the prefix byte length, or 0.</param>
        /// <returns><see langword="true"/> if the buffer holds that local name followed by a name terminator.</returns>
        /// <remarks>
        /// Handles <c>&lt;/local&gt;</c> and <c>&lt;/prefix:local&gt;</c>. Does not advance <c>_pos</c> on failure.
        /// </remarks>
        internal bool TryMatchEndName(out int nameStart, out int nameLength, out int prefixLength)
        {
            nameStart = _pos;
            nameLength = 0;
            prefixLength = 0;
            if (_elementCount == 0)
            {
                return false;
            }

            var expected = ElementLocalName(_elementCount - 1);
            var localStart = _pos;
            var rest = _span[_pos..];
            var stop = rest.IndexOfAny(XmlChars.EndTagNameStops);
            if (stop > 0 && stop < rest.Length && rest[stop] is (byte)':' && stop + 1 < rest.Length)
            {
                var afterColon = rest[stop + 1];
                if (afterColon < 0x80 && XmlChars.IsAsciiNameStart(afterColon) && afterColon is not (byte)':')
                {
                    localStart = _pos + stop + 1;
                    prefixLength = stop;
                }
            }

            if (localStart + expected.Length > _span.Length)
            {
                return false;
            }

            if (!_span.Slice(localStart, expected.Length).SequenceEqual(expected))
            {
                return false;
            }

            var after = localStart + expected.Length;
            if (after >= _span.Length)
            {
                return false;
            }

            var n = _span[after];
            if (n >= 0x80 || XmlChars.IsAsciiNcNameChar(n) || n is (byte)':')
            {
                return false;
            }

            nameLength = after - nameStart;
            _pos = after;
            return true;
        }

        /// <summary>
        /// Matches an unprefixed end tag against the current element's local name without a full name scan.
        /// </summary>
        /// <param name="nameStart">When this method returns true, the byte offset of the local name.</param>
        /// <param name="nameLength">When this method returns true, the local-name byte length.</param>
        /// <returns><see langword="true"/> if the buffer holds that local name followed by a name terminator.</returns>
        internal bool TryMatchUnprefixedEndName(out int nameStart, out int nameLength)
        {
            return TryMatchEndName(out nameStart, out nameLength, out _);
        }

        /// <summary>
        /// Returns the trimmed current value if it is a plain integer lexical form.
        /// </summary>
        /// <param name="span">When this method returns, the trimmed UTF-8 digits.</param>
        /// <returns><see langword="false"/> if the value uses <c>+</c>, exponent, or grouping.</returns>
        internal readonly bool TryGetIntegerUtf8(out ReadOnlySpan<byte> span)
        {
            span = TrimXmlWhitespace(ValueSpan);
            if (span.IsEmpty || span[0] is (byte)'+' || ContainsForbiddenIntegerByte(span))
            {
                span = default;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses a decimal or hexadecimal code point from UTF-8 digits.
        /// </summary>
        /// <param name="digits">The ASCII digits.</param>
        /// <param name="hex">True to parse hexadecimal.</param>
        /// <param name="value">When this method returns, the code point.</param>
        /// <returns><see langword="true"/> if the digits formed an integer.</returns>
        internal static bool TryParseCodePoint(ReadOnlySpan<byte> digits, bool hex, out int value)
        {
            value = 0;
            if (digits.IsEmpty)
            {
                return false;
            }

            foreach (var b in digits)
            {
                int digit;
                if (b is >= (byte)'0' and <= (byte)'9')
                {
                    digit = b - (byte)'0';
                }
                else if (hex && b is >= (byte)'a' and <= (byte)'f')
                {
                    digit = b - (byte)'a' + 10;
                }
                else if (hex && b is >= (byte)'A' and <= (byte)'F')
                {
                    digit = b - (byte)'A' + 10;
                }
                else
                {
                    return false;
                }

                if (value > (0x10FFFF - digit) / (hex ? 16 : 10))
                {
                    return false;
                }

                value = (value * (hex ? 16 : 10)) + digit;
            }

            return true;
        }

        /// <summary>
        /// Parses XSD <c>duration</c> day/time forms into a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="text">The lexical value, such as <c>PT1H2M3S</c>.</param>
        /// <param name="value">When this method returns, the parsed duration.</param>
        /// <returns><see langword="true"/> if <paramref name="text"/> is a duration that fits in <see cref="TimeSpan"/>.</returns>
        /// <remarks>
        /// Year and month components are rejected. Negative durations are accepted.
        /// </remarks>
        internal static bool TryParseDuration(ReadOnlySpan<char> text, out TimeSpan value)
        {
            value = default;
            if (text.Length == 0 || text[0] is not 'P' and not '-')
            {
                return false;
            }

            try
            {
                var negative = text[0] is '-';
                var s = negative ? text[1..] : text;
                if (s.Length == 0 || s[0] is not 'P')
                {
                    return false;
                }

                s = s[1..];
                var days = 0;
                var hours = 0;
                var minutes = 0;
                var seconds = 0d;
                var inTime = false;
                var parsed = false;
                var i = 0;
                while (i < s.Length)
                {
                    if (s[i] is 'T')
                    {
                        inTime = true;
                        i++;
                        continue;
                    }

                    var start = i;
                    while (i < s.Length && (char.IsAsciiDigit(s[i]) || s[i] is '.'))
                    {
                        i++;
                    }

                    if (i >= s.Length || start == i)
                    {
                        return false;
                    }

                    var number = s[start..i];
                    var unit = s[i];
                    i++;
                    switch (unit)
                    {
                        case 'D':
                            days = int.Parse(number, CultureInfo.InvariantCulture);
                            parsed = true;
                            break;
                        case 'H' when inTime:
                            hours = int.Parse(number, CultureInfo.InvariantCulture);
                            parsed = true;
                            break;
                        case 'M' when inTime:
                            minutes = int.Parse(number, CultureInfo.InvariantCulture);
                            parsed = true;
                            break;
                        case 'S' when inTime:
                            seconds = double.Parse(number, CultureInfo.InvariantCulture);
                            parsed = true;
                            break;
                        case 'Y' or 'M' when !inTime:
                            return false;
                        default:
                            return false;
                    }
                }

                if (!parsed)
                {
                    return false;
                }

                value = TimeSpan.FromDays(days) + TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
                if (negative)
                {
                    value = -value;
                }

                return true;
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to parse the current value as an XSD integer using <paramref name="tryParse"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The integer type.
        /// </typeparam>
        /// <param name="tryParse">The framework try-parse function.</param>
        /// <param name="value">When this method returns, the parsed value.</param>
        /// <returns><see langword="true"/> if parsing succeeded.</returns>
        internal bool TryParseInteger<T>(TryParseHandler<T> tryParse, out T value)
            where T : struct
        {
            var span = TrimXmlWhitespace(ValueSpan);
            if (span.IsEmpty || span[0] is (byte)'+' || ContainsForbiddenIntegerByte(span))
            {
                value = default;
                return false;
            }

            Span<char> chars = stackalloc char[span.Length];
            var n = Encoding.UTF8.GetChars(span, chars);
            return tryParse(new string(chars[..n]), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// Returns true if <paramref name="span"/> contains exponent or grouping characters.
        /// </summary>
        /// <param name="span">The UTF-8 integer lexical value.</param>
        /// <returns><see langword="true"/> if the span is not a plain integer.</returns>
        internal static bool ContainsForbiddenIntegerByte(ReadOnlySpan<byte> span)
        {
            return span.IndexOfAny((byte)'e', (byte)'E') >= 0
                || span.IndexOfAny((byte)'_', (byte)',') >= 0;
        }

        /// <summary>
        /// Dispatches <c>&lt;!</c> markup to comment, CDATA, or a DTD rejection.
        /// </summary>
        /// <returns><see langword="true"/> if a comment or CDATA token was completed; <see langword="false"/> if more data is required.</returns>
        /// <exception cref="XmlException">The markup is a DTD construct, which XmlEssentials rejects.</exception>
        internal bool TryReadMarkupDeclaration()
        {
            var remaining = _span.Slice(_pos);
            if (remaining.StartsWith("<!--"u8))
            {
                return ReadComment();
            }

            if (remaining.StartsWith("<![CDATA["u8))
            {
                return ReadCData();
            }

            if (!_isFinalBlock
                && remaining.Length < 9
                && remaining.StartsWith("<!"u8)
                && !remaining.StartsWith("<!D"u8)
                && !remaining.StartsWith("<!E"u8)
                && !remaining.StartsWith("<!A"u8)
                && !remaining.StartsWith("<!N"u8))
            {
                return NeedMore();
            }

            throw Error("DTD is not allowed.");
        }

        /// <summary>
        /// Rejects characters that are illegal in the current document version.
        /// </summary>
        /// <param name="cp">The Unicode code point.</param>
        /// <param name="fromNcr">True when <paramref name="cp"/> came from a numeric character reference.</param>
        /// <exception cref="XmlException">NUL, a non-Char, or a 1.1 RestrictedChar written as a literal.</exception>
        /// <remarks>
        /// XML 1.1 RestrictedChar is legal only as an NCR. In 1.0 the same code points are illegal in every form.
        /// </remarks>
        internal void ValidateChar(int cp, bool fromNcr)
        {
            if (cp is 0)
            {
                throw Error("NUL is not allowed.");
            }

            if (_version is XmlVersion.Version10)
            {
                if (!XmlChars.IsXml10Char(cp))
                {
                    throw Error("Invalid XML 1.0 character.");
                }

                return;
            }

            if (!XmlChars.IsXml11Char(cp))
            {
                throw Error("Invalid XML 1.1 character.");
            }

            if (!fromNcr && XmlChars.IsXml11RestrictedChar(cp))
            {
                throw Error("XML 1.1 RestrictedChar must be a character reference.");
            }
        }

        #endregion

    }

}
