using System;
using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

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
            var lineEnd = _span[_pos] is (byte)'\n' ? 1 : LineEndToReplace(_pos, _span.Length);
            if (lineEnd > 0)
            {
                _pos += lineEnd;
                _lineNumber++;
                _lineStart = _pos;
                return;
            }

            var (cp, width) = DecodeRune(_pos);
            if (width == 0)
            {
                // The buffer ends inside a multi-byte character. Consume the rest so the caller runs out
                // of input and reports that more data is needed.
                _pos = _span.Length;
                return;
            }

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

            // The rest of the run holds multi-byte characters. Check all of it in one pass; anything
            // that fails is left for AdvanceChar, which reports the exact error.
            // XML 1.1 adds characters that need AdvanceChar: the C1 controls must be references, and
            // NEL and LS are line ends. Their encodings all start with 0xC2 or 0xE2.
            var tail = run[bad..];
            if (tail.IndexOfAnyExcept(XmlChars.TextRunChars) < 0
                && Utf8.IsValid(tail)
                && !XmlChars.ContainsNonCharacter(tail)
                && (_version is XmlVersion.Version10 || tail.IndexOfAny((byte)0xC2, (byte)0xE2) < 0))
            {
                _pos += length;
                return true;
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
        /// Copies the text that has been scanned but not yet copied into the decode buffer, followed by what
        /// replaces the reference or line end after it.
        /// </summary>
        /// <param name="rawStart">Offset of the first input byte not yet copied.</param>
        /// <param name="rawEnd">Offset just past the last input byte to copy as it is.</param>
        /// <param name="replacement">The bytes that stand for what follows the run, or empty at the end of the text.</param>
        /// <param name="written">The number of bytes in the decode buffer, updated.</param>
        /// <remarks>
        /// <see cref="BeginTextDecode"/> sized the buffer for the whole token, and neither a reference nor a
        /// line end is ever replaced by something longer, so there is always room.
        /// </remarks>
        internal void AppendDecodedText(int rawStart, int rawEnd, scoped ReadOnlySpan<byte> replacement, ref int written)
        {
            var run = rawEnd - rawStart;
            var destination = _decodedValue[written..];
            _span.Slice(rawStart, run).CopyTo(destination);
            replacement.CopyTo(destination[run..]);
            written += run + replacement.Length;
        }

        /// <summary>
        /// Readies the decode buffer for a text token whose first reference or replaced line end is at the
        /// current position.
        /// </summary>
        /// <param name="start">Offset of the first byte of the text.</param>
        /// <param name="written">Set to zero when decoding can start.</param>
        /// <returns>
        /// <see langword="false"/> when the text does not end in this buffer and more data may arrive. The
        /// whole token is read again from the next buffer, so nothing is decoded or reserved for it now.
        /// </returns>
        /// <exception cref="XmlException">The text is longer than <see cref="XmlReaderOptions.MaxTextByteCount"/>.</exception>
        internal bool BeginTextDecode(int start, ref int written)
        {
            // Text cannot hold '<', so the next one is where this token ends.
            var rest = _span[_pos..].IndexOf((byte)'<');
            if (rest < 0)
            {
                if (!_isFinalBlock)
                {
                    return false;
                }

                rest = _span.Length - _pos;
            }

            var length = _pos + rest - start;
            if (length > _maxTextBytes)
            {
                throw TextTooLong();
            }

            EnsureDecodedCapacity(length);
            written = 0;
            return true;
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
            _resumePrefixes = new byte[snapshot.ElementLocalNames.Length][];
            _resumeNsUris = new byte[snapshot.ElementLocalNames.Length][];
            _nsCount = 0;
            _nsBindings = default;
            for (var i = 0; i < snapshot.ElementLocalNames.Length; i++)
            {
                _resumeLocalNames[i] = Encoding.UTF8.GetBytes(snapshot.ElementLocalNames[i]);
                _resumePrefixes[i] = Encoding.UTF8.GetBytes(snapshot.ElementPrefixes[i]);
                _resumeNsUris[i] = InternUri(Encoding.UTF8.GetBytes(snapshot.ElementNamespaceUris[i]));
                _elementCount = i;
                EnsureElementCapacity();
                SetElementSlot(i, snapshot.ElementFlags[i]);
            }

            _elementCount = snapshot.ElementLocalNames.Length;

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
            _isOnOpenStartTag = snapshot.IsOnOpenStartTag;
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
                ElementPrefixes = MaterializeElementPrefixes(),
                ElementFlags = MaterializeElementFlags(),
                ElementNamespaceUris = MaterializeElementNamespaces(),
                Finished = _finished,
                IsEmptyElement = _isEmptyElement,
                IsOnOpenStartTag = _isOnOpenStartTag,
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

            return _attributes[_attributeIndex];
        }

        /// <summary>
        /// Returns whether the current attribute is in no namespace and has the name <paramref name="utf8Name"/>.
        /// </summary>
        /// <param name="utf8Name">The UTF-8 attribute name, with no prefix.</param>
        /// <returns><see langword="true"/> when the names are equal, byte for byte.</returns>
        /// <remarks>
        /// An attribute with no prefix is in no namespace, so the whole name is compared at once. A prefixed
        /// attribute and an <c>xmlns</c> declaration never match. Callers must have selected an attribute with
        /// <see cref="MoveToNextAttribute"/>.
        /// </remarks>
        internal readonly bool UnqualifiedAttributeNameEquals(ReadOnlySpan<byte> utf8Name)
        {
            var attribute = CurrentAttribute();
            return attribute.NsIndex == AttributeRecord.NoNamespace
                && _span.Slice(attribute.NameStart, attribute.NameLength).SequenceEqual(utf8Name);
        }

        /// <summary>
        /// Returns the qualified name of the current element token, including a prefix and colon when present.
        /// </summary>
        /// <param name="prefixLength">When this method returns, the prefix byte length, or 0 when unprefixed.</param>
        /// <returns>UTF-8 name bytes in the current buffer.</returns>
        /// <remarks>
        /// One slice for <c>prefix:local</c> lets <see cref="XmlDocument"/> record a name with a single offset.
        /// </remarks>
        internal readonly ReadOnlySpan<byte> CurrentQualifiedName(out int prefixLength)
        {
            prefixLength = _prefixLength;
            return _span.Slice(_nameStart, _nameLength);
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
        /// <param name="decodedStart">Offset of the decoded value in the returned buffer.</param>
        /// <param name="decodedLength">Byte length of the decoded value, or <paramref name="length"/> when no decode ran.</param>
        /// <returns><see langword="true"/> when the value was decoded; <see langword="false"/> when the raw slice is already the value.</returns>
        /// <remarks>
        /// TAB, LF, and CR become SPACE after end-of-line handling, per attribute-value normalization.
        /// </remarks>
        internal bool DecodeAttributeValue(int start, int length, out bool escaped, out int decodedStart, out int decodedLength)
        {
            escaped = false;
            decodedStart = 0;
            decodedLength = length;
            var slice = _span.Slice(start, length);
            var xml11Eol = _version is XmlVersion.Version11 && ContainsXml11LineEnding(slice);
            if (slice.IndexOfAny(XmlChars.AttrDecodeTriggers) < 0 && !xml11Eol)
            {
                return false;
            }

            var savedPos = _pos;
            var destination = RentAttributeDecodeSpace(length);
            var written = 0;
            var i = start;
            Span<byte> entityScratch = stackalloc byte[8];
            while (i < start + length)
            {
                _pos = i;
                if (_span[i] is (byte)'&')
                {
                    // ReadAttribute already consumed this reference once, so it is complete and legal.
                    DecodeEntity(entityScratch, out var entityWritten);
                    entityScratch[..entityWritten].CopyTo(destination[written..]);
                    written += entityWritten;
                    escaped = true;
                    i = _pos;
                    continue;
                }

                var lineEnd = LineEndToReplace(i, start + length);
                if (lineEnd > 0 || _span[i] is (byte)'\t' or (byte)'\n')
                {
                    // A line end is one LF after end-of-line handling, and normalization makes that one space.
                    destination[written] = (byte)' ';
                    written++;
                    escaped = true;
                    i += Math.Max(lineEnd, 1);
                    continue;
                }

                destination[written] = _span[i];
                written++;
                i++;
            }

            _pos = savedPos;
            escaped = true;
            decodedLength = written;
            decodedStart = _attributeDecodedLength;
            _attributeDecodedLength += written;
            return true;
        }

        /// <summary>
        /// Reserves room for one decoded attribute value in the buffer shared by the current start tag.
        /// </summary>
        /// <param name="length">The raw value length, which bounds the decoded length.</param>
        /// <returns>The free space after the values already decoded for this start tag.</returns>
        /// <remarks>
        /// The buffer is reused from one start tag to the next, so a document with escaped attribute
        /// values costs one array per reader rather than one per attribute. When it has to grow, earlier
        /// attributes of the same tag keep the array they were decoded into.
        /// </remarks>
        internal Span<byte> RentAttributeDecodeSpace(int length)
        {
            if (_attributeDecodedLength + length > _attributeDecoded.Length)
            {
                // Earlier attributes of this start tag keep their offsets, so what they decoded moves with them.
                var size = Math.Max(Math.Max(256, _attributeDecodedLength + length), _attributeDecoded.Length * 2);
                _attributeDecoded = Grow<byte>(_attributeDecoded, _attributeDecodedLength, size);
            }

            return _attributeDecoded.Slice(_attributeDecodedLength, length);
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
                var hex = _pos < _span.Length && _span[_pos] is (byte)'x';
                if (hex)
                {
                    _pos++;
                }

                var numberStart = _pos;
                while (_pos < _span.Length && IsHexOrDec(_span[_pos], hex))
                {
                    _pos++;
                }

                if (_pos >= _span.Length)
                {
                    _pos = start;
                    return false;
                }

                if (_span[_pos] is not (byte)';')
                {
                    // CharRef is '&#' [0-9]+ ';' or '&#x' [0-9a-fA-F]+ ';' (production 66). The 'x' is lower case.
                    throw Error("Invalid character reference.");
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


            destination[0] = replacement;
            written = 1;
            return true;
        }

        /// <summary>
        /// Decodes one Unicode code point at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">Byte offset in the current buffer.</param>
        /// <returns>
        /// The code point and the number of UTF-8 bytes it occupies. A width of 0 means the buffer ends inside
        /// the character and more data may still arrive.
        /// </returns>
        /// <exception cref="XmlException">The bytes are not valid UTF-8.</exception>
        internal (int CodePoint, int Width) DecodeRune(int index)
        {
            var span = _span.Slice(index);
            if (!span.IsEmpty && span[0] < 0x80)
            {
                return (span[0], 1);
            }

            var status = Rune.DecodeFromUtf8(span, out var rune, out var consumed);
            if (status is OperationStatus.NeedMoreData && !_isFinalBlock)
            {
                return (0, 0);
            }

            if (status is not OperationStatus.Done)
            {
                throw Error("Invalid UTF-8.");
            }

            return (rune.Value, consumed);
        }

        /// <summary>
        /// Normalizes line ends into <c>_decodedValue</c>. A value that has none to replace stays in the input.
        /// </summary>
        /// <param name="start">Byte offset of the raw value in the current buffer.</param>
        /// <param name="length">UTF-8 byte length of the raw value.</param>
        /// <remarks>
        /// For CDATA sections, comments, and processing instructions, where <c>&amp;</c> is an ordinary
        /// character. Text is decoded by <see cref="ReadText"/> as it is scanned.
        /// </remarks>
        internal void DecodeValueSpan(int start, int length)
        {
            var end = start + length;
            var stops = _version is XmlVersion.Version11 ? XmlChars.DecodeStops11 : XmlChars.DecodeStops10;
            var i = start + FirstLineEndToReplace(start, end, stops);
            if (i == end)
            {
                _valueIsEscaped = false;
                return;
            }

            EnsureDecodedCapacity(length);
            var destination = _decodedValue;
            _span[start..i].CopyTo(destination);
            var written = i - start;
            while (i < end)
            {
                destination[written] = (byte)'\n';
                written++;
                i += LineEndToReplace(i, end);

                // Everything up to the next line end is copied as it is, in one piece.
                var run = FirstLineEndToReplace(i, end, stops);
                _span.Slice(i, run).CopyTo(destination[written..]);
                written += run;
                i += run;
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

            if (index < InlineSlots)
            {
                return _span.Slice(_elementNameStartsInline[index], _elementNameLengthsInline[index]);
            }

            return _span.Slice(_elementNameStarts[index], _elementNameLengths[index]);
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
            if (_decodedValue.Length >= needed)
            {
                return;
            }

            // At least double, so values that keep getting longer cost a logarithmic number of buffers.
            _decodedValue = Grow<byte>(default, 0, Math.Max(needed, _decodedValue.Length * 2));
        }

        /// <summary>
        /// Makes sure the owned-URI stack has a slot for <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The element-stack index.</param>
        /// <remarks>
        /// Only an element whose namespace URI is not in the current buffer, because it was decoded or came
        /// from an earlier block, uses a slot, so most readers never allocate this stack.
        /// </remarks>
        internal void EnsureElementNsUriCapacity(int index)
        {
            if (_elementNsUris is null || index >= _elementNsUris.Length)
            {
                Array.Resize(ref _elementNsUris, Math.Max(InlineSlots, (index + 1) * 2));
            }
        }

        /// <summary>
        /// Grows the element-name slice stacks.
        /// </summary>
        internal void EnsureElementCapacity()
        {
            if (_elementCount < InlineSlots)
            {
                return;
            }

            if (!_elementNameStarts.IsEmpty && _elementCount < _elementNameStarts.Length)
            {
                return;
            }

            var n = _elementNameStarts.IsEmpty ? InlineSlots * 2 : _elementNameStarts.Length * 2;
            _elementNameStarts = Grow<int>(_elementNameStarts, _elementNameStarts.Length, n);
            _elementNameLengths = Grow<int>(_elementNameLengths, _elementNameLengths.Length, n);
            _elementPrefixLengths = Grow<int>(_elementPrefixLengths, _elementPrefixLengths.Length, n);
            if (n == InlineSlots * 2)
            {
                for (var i = 0; i < InlineSlots; i++)
                {
                    _elementNameStarts[i] = _elementNameStartsInline[i];
                    _elementNameLengths[i] = _elementNameLengthsInline[i];
                    _elementPrefixLengths[i] = _elementPrefixLengthsInline[i];
                }
            }
        }

        /// <summary>
        /// Gives <paramref name="exception"/> the current byte offset and line when it has no position.
        /// </summary>
        /// <param name="exception">An exception that was thrown while this reader was being read from.</param>
        /// <remarks>
        /// The serializer calls this for an exception that did not come from the reader, such as a value that
        /// is not of the type it is read as. The position is where the reader had got to.
        /// </remarks>
        internal readonly void Locate(XmlException exception)
        {
            exception.SetPositionIfMissing(_documentBytesConsumedBase + _pos, _lineNumber, _pos - _lineStart + 1);
        }

        /// <summary>
        /// Creates an <see cref="XmlException"/> at the current byte offset and line.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The exception. Callers throw it.</returns>
        internal readonly XmlException Error(string message)
        {
            var linePosition = _pos - _lineStart + 1;
            return XmlException.Create(message, _documentBytesConsumedBase + _pos, _lineNumber, linePosition, path: null);
        }

        /// <summary>
        /// Returns how far the first line end that has to be replaced is from <paramref name="from"/>.
        /// </summary>
        /// <param name="from">Offset to search from.</param>
        /// <param name="end">Offset just past the value.</param>
        /// <param name="stops">The bytes a replaced line end can start with in this XML version.</param>
        /// <returns>The distance in bytes, or the distance to <paramref name="end"/> when there is none.</returns>
        internal readonly int FirstLineEndToReplace(int from, int end, SearchValues<byte> stops)
        {
            var i = from;
            while (i < end)
            {
                var next = _span[i..end].IndexOfAny(stops);
                if (next < 0)
                {
                    break;
                }

                i += next;
                if (LineEndToReplace(i, end) > 0)
                {
                    return i - from;
                }

                // The lead byte of some other character.
                i++;
            }

            return end - from;
        }

        /// <summary>
        /// Returns the length of the line end at <paramref name="index"/> that end-of-line handling replaces
        /// with a single LF.
        /// </summary>
        /// <param name="index">Offset of the byte to look at.</param>
        /// <param name="end">Offset just past the bytes that may be read.</param>
        /// <returns>
        /// The number of bytes to replace, or zero when the byte does not start such a line end. LF alone is
        /// already normalized and returns zero.
        /// </returns>
        /// <remarks>
        /// XML 1.0 section 2.11 replaces CR LF and a lone CR. XML 1.1 section 2.11 also replaces CR NEL
        /// (as one line end), NEL (U+0085), and LS (U+2028).
        /// </remarks>
        internal readonly int LineEndToReplace(int index, int end)
        {
            var b = _span[index];
            if (b is (byte)'\r')
            {
                if (index + 1 < end && _span[index + 1] is (byte)'\n')
                {
                    return 2;
                }

                if (_version is XmlVersion.Version11 && index + 2 < end && _span[index + 1] is 0xC2 && _span[index + 2] is 0x85)
                {
                    return 3;
                }

                return 1;
            }

            if (_version is XmlVersion.Version10)
            {
                return 0;
            }

            if (b is 0xC2 && index + 1 < end && _span[index + 1] is 0x85)
            {
                return 2;
            }

            if (b is 0xE2 && index + 2 < end && _span[index + 1] is 0x80 && _span[index + 2] is 0xA8)
            {
                return 3;
            }

            return 0;
        }

        /// <summary>
        /// Returns memory for <paramref name="count"/> items, taken from the caller's scratch buffer while it
        /// lasts and allocated after that, holding the first <paramref name="keep"/> items of <paramref name="old"/>.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="old">The memory being replaced, or empty.</param>
        /// <param name="keep">The number of items to carry over.</param>
        /// <param name="count">The number of items needed.</param>
        /// <returns>The new memory. Items after the first <paramref name="keep"/> are zero.</returns>
        /// <remarks>
        /// Memory taken from the scratch buffer is never given back: a stack or buffer that grows leaves the
        /// part it outgrew unused. Each one at least doubles, so that waste is less than what is in use.
        /// </remarks>
        internal Span<T> Grow<T>(scoped ReadOnlySpan<T> old, int keep, int count)
            where T : unmanaged
        {
            Span<T> grown;
            // Rounded up to eight bytes from the start of the scratch span. That start is the payload
            // length, so this is packing, not a guaranteed natural alignment of T.
            var offset = (_scratchUsed + 7) & ~7;
            var size = (long)Unsafe.SizeOf<T>() * count;
            if (size <= _scratch.Length - offset)
            {
                grown = MemoryMarshal.Cast<byte, T>(_scratch.Slice(offset, (int)size));
                grown[keep..].Clear();
                _scratchUsed = offset + (int)size;
            }
            else
            {
                grown = new T[count];
            }

            old[..keep].CopyTo(grown);
            return grown;
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
            var slot = ElementSlot(_elementCount - 1);
            var significant = _options.WhitespaceHandling is XmlWhitespaceHandling.Significant
                || (slot & (ElementHasText | ElementPreserveSpace)) != 0;
            if (!significant && (slot & ElementHasChild) == 0)
            {
                // No child element and no text so far. This whitespace is the element's whole value
                // if the end tag comes next (<c> </c>); otherwise it is indentation before a child.
                if (_pos + 1 >= _span.Length)
                {
                    return NeedMore();
                }

                significant = _span[_pos + 1] is (byte)'/';
            }

            if (significant)
            {
                SetElementSlot(_elementCount - 1, slot | ElementHasText);
                _nameLength = 0;
                _prefixLength = 0;
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
        /// Returns the packed stack slot of the open element at <paramref name="index"/>: its prefix length and flags.
        /// </summary>
        /// <param name="index">The element-stack index.</param>
        /// <returns>The slot value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int ElementSlot(int index)
        {
            return index < InlineSlots ? _elementPrefixLengthsInline[index] : _elementPrefixLengths[index];
        }

        /// <summary>
        /// Stores the packed stack slot of the open element at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The element-stack index.</param>
        /// <param name="value">The prefix length combined with the element flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetElementSlot(int index, int value)
        {
            if (index < InlineSlots)
            {
                _elementPrefixLengthsInline[index] = value;
            }
            else
            {
                _elementPrefixLengths[index] = value;
            }
        }

        /// <summary>
        /// Applies an <c>xml:space</c> attribute on the current start tag to the inherited setting.
        /// </summary>
        /// <param name="inherited">The parent's <see cref="ElementPreserveSpace"/> bit, or 0.</param>
        /// <returns>The bit for this element: set for <c>preserve</c>, clear for <c>default</c>, otherwise inherited.</returns>
        /// <remarks>
        /// XML 1.0 and 1.1 section 2.10: the declared intent applies to the element and everything in it
        /// until another <c>xml:space</c> overrides it.
        /// </remarks>
        internal readonly int ResolveXmlSpace(int inherited)
        {
            // The caller saw an xml:space attribute while reading this tag, so the loop always finds it.
            for (var i = 0; ; i++)
            {
                var attribute = AttributeAt(i);
                if (!AttributeQualifiedName(in attribute).SequenceEqual("xml:space"u8))
                {
                    continue;
                }

                var value = AttributeValue(attribute);
                if (value.SequenceEqual("preserve"u8))
                {
                    return ElementPreserveSpace;
                }

                return value.SequenceEqual("default"u8) ? 0 : inherited;
            }
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
        /// Copies the whitespace-relevant flags of each open element for a snapshot.
        /// </summary>
        /// <returns>One flag set per open element, without the prefix length (which refers to the old buffer).</returns>
        internal readonly int[] MaterializeElementFlags()
        {
            var flags = new int[_elementCount];
            for (var i = 0; i < _elementCount; i++)
            {
                flags[i] = ElementSlot(i) & ~ElementPrefixLengthMask;
            }

            return flags;
        }

        /// <summary>
        /// Returns the prefix the in-progress element at <paramref name="index"/> was started with.
        /// </summary>
        /// <param name="index">The element-stack index.</param>
        /// <returns>UTF-8 prefix bytes from the current buffer or a resume copy, or empty.</returns>
        internal readonly ReadOnlySpan<byte> ElementPrefix(int index)
        {
            if (index < _resumeNameCount && _resumePrefixes is not null)
            {
                return _resumePrefixes[index];
            }

            var prefixLength = ElementSlot(index) & ElementPrefixLengthMask;
            if (prefixLength == 0)
            {
                return [];
            }

            var localStart = index < InlineSlots ? _elementNameStartsInline[index] : _elementNameStarts[index];
            return _span.Slice(localStart - prefixLength - 1, prefixLength);
        }

        /// <summary>
        /// Copies the prefixes of in-progress elements to independent strings for a snapshot.
        /// </summary>
        /// <returns>One string per open element.</returns>
        internal readonly string[] MaterializeElementPrefixes()
        {
            var prefixes = new string[_elementCount];
            for (var i = 0; i < _elementCount; i++)
            {
                prefixes[i] = Encoding.UTF8.GetString(ElementPrefix(i));
            }

            return prefixes;
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
        /// Validates the current XML declaration token and applies its version.
        /// </summary>
        /// <exception cref="XmlException">The declaration does not match the XMLDecl production, or names an encoding other than UTF-8.</exception>
        internal void ParseXmlDeclaration()
        {
            if (!XmlDeclarationSyntax.TryParse(_span.Slice(_valueStart, _valueLength), out var version, out var encoding, out _))
            {
                throw Error("The XML declaration is malformed.");
            }

            _version = version;
            if (_options.IgnoreDeclaredEncoding)
            {
                // The document arrived as characters, so the encoding it names was for some other copy of it.
                return;
            }

            if (!encoding.IsEmpty && !Ascii.EqualsIgnoreCase(encoding, "utf-8"u8) && !Ascii.EqualsIgnoreCase(encoding, "utf8"u8))
            {
                throw Error($"XmlEssentials is UTF-8 only. Declared encoding '{Encoding.UTF8.GetString(encoding).ToLowerInvariant()}' is not supported.");
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

            var nsIndex = AttributeRecord.NoNamespace;
            if (nameLength >= 5
                && (prefixLength == 5 || (prefixLength == 0 && nameLength == 5))
                && _span.Slice(nameStart, 5).SequenceEqual("xmlns"u8))
            {
                _hasNamespaceAttribute = true;
                nsIndex = AttributeRecord.XmlnsNamespace;
            }
            else if (prefixLength == 3 && _span.Slice(nameStart, 3).SequenceEqual("xml"u8))
            {
                nsIndex = AttributeRecord.XmlNamespace;
                _hasXmlSpaceAttribute |= nameLength == 9 && _span.Slice(nameStart + 4, 5).SequenceEqual("space"u8);
            }
            else if (prefixLength > 0)
            {
                nsIndex = AttributeRecord.UnresolvedNamespace;
                _hasUnresolvedAttribute = true;
            }

            _pos++;
            var valueStart = _pos;
            var stops = quote is (byte)'"' ? XmlChars.AttrStopsDouble : XmlChars.AttrStopsSingle;

            // Most values are plain ASCII up to the closing quote. One scan proves that, and then the
            // value needs no character check, no reference expansion, and no normalization.
            var plainLength = _span[_pos..].IndexOfAnyExcept(quote is (byte)'"' ? XmlChars.AttrPlainDouble : XmlChars.AttrPlainSingle);
            var plain = plainLength >= 0 && _span[_pos + plainLength] == quote;
            if (plainLength > 0)
            {
                _pos += plainLength;
            }

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
            var decoded = false;
            var escaped = false;
            var decodedStart = 0;
            var decodedLength = valueLength;
            if (!plain)
            {
                decoded = DecodeAttributeValue(valueStart, valueLength, out escaped, out decodedStart, out decodedLength);
            }

            var record = new AttributeRecord
            {
                DecodedStart = decoded ? decodedStart : -1,
                DecodedLength = decodedLength,
                NameLength = nameLength,
                NameStart = nameStart,
                NsIndex = nsIndex,
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
                if (_attributes.IsEmpty)
                {
                    _attributes = Grow<AttributeRecord>(default, 0, 16);
                    for (var i = 0; i < 8; i++)
                    {
                        _attributes[i] = _attributesInline[i];
                    }
                }
                else if (_attributeCount == _attributes.Length)
                {
                    _attributes = Grow<AttributeRecord>(_attributes, _attributeCount, _attributes.Length * 2);
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

                    DecodeValueSpan(start, _valueLength);
                    _nameLength = 0;
                    _prefixLength = 0;
                    if (_elementCount > 0)
                    {
                        SetElementSlot(_elementCount - 1, ElementSlot(_elementCount - 1) | ElementHasText);
                    }

                    _tokenType = XmlTokenType.CData;
                    return true;
                }

                // A run of plain ASCII that cannot start the terminator needs no per-character check.
                // Line ends, ']' and everything else go through AdvanceChar.
                var plain = _span[_pos..].IndexOfAnyExcept(XmlChars.CDataPlainChars);
                if (plain == 0)
                {
                    AdvanceChar();
                }
                else
                {
                    _pos = plain < 0 ? _span.Length : _pos + plain;
                }
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

                    DecodeValueSpan(start, _valueLength);
                    _nameLength = 0;
                    _prefixLength = 0;
                    _tokenType = XmlTokenType.Comment;
                    return true;
                }

                // A run of plain ASCII that cannot start the terminator needs no per-character check.
                // Line ends, '-' and everything else go through AdvanceChar.
                var plain = _span[_pos..].IndexOfAnyExcept(XmlChars.CommentPlainChars);
                if (plain == 0)
                {
                    AdvanceChar();
                }
                else
                {
                    _pos = plain < 0 ? _span.Length : _pos + plain;
                }
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
            var sameAsStartTag = TryMatchStartTagName(out nameStart, out nameLength, out prefixLength);
            if (!sameAsStartTag && !(_elementCount > 0 && TryMatchEndName(out nameStart, out nameLength, out prefixLength)))
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

            if (_pos >= _span.Length || _span[_pos] is not (byte)'>')
            {
                SkipWhitespaceBytes();
                if (_pos >= _span.Length)
                {
                    return NeedMore();
                }

                if (_span[_pos] is not (byte)'>')
                {
                    throw Error("Expected '>' in end tag.");
                }
            }

            _pos++;
            var index = _elementCount - 1;
            if (sameAsStartTag)
            {
                RestoreCurrentNamespaceFromElement(index);
            }
            else
            {
                var localAdj = prefixLength > 0 ? prefixLength + 1 : 0;
                var local = _span.Slice(nameStart + localAdj, nameLength - localAdj);
                if (_elementCount == 0 || !ElementLocalName(index).SequenceEqual(local))
                {
                    throw Error("End tag does not match start tag.");
                }

                // The local names match but the tag was not compared with the start tag's bytes, because
                // the start tag came from a resume snapshot or the prefix differs. The Name in an end tag
                // must match the start tag's (XML 1.0 section 3, WFC: Element Type Match), and the prefix
                // is part of the Name: a different prefix is an error even when it is bound to the same
                // namespace.
                var prefix = prefixLength > 0 ? _span.Slice(nameStart, prefixLength) : [];
                if (!ElementPrefix(index).SequenceEqual(prefix))
                {
                    throw Error("End tag does not match start tag.");
                }

                SetCurrentNamespace(ElementNamespace(index));
            }

            PopNamespaceScope(_depth);
            _elementCount--;
            if (_elementCount < _resumeNameCount)
            {
                // A resumed element just closed. The next element at this depth is read from the
                // current buffer and must not see the closed element's saved name or namespace.
                _resumeNameCount = _elementCount;
            }

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
                if (!XmlChars.IsAsciiNameStart(b) || b is (byte)':')
                {
                    return false;
                }

                _pos++;
            }
            else
            {
                var (cp, width) = DecodeRune(_pos);
                if (width == 0)
                {
                    return NeedMore();
                }

                if (!XmlChars.IsNameStartChar(cp, _version))
                {
                    return false;
                }

                _pos += width;
            }

            while (_pos < _span.Length)
            {
                // Names are short, so a byte loop beats a vectorized search here.
                var p = _pos;
                while ((uint)p < (uint)_span.Length && XmlChars.IsAsciiNcNameChar(_span[p]))
                {
                    p++;
                }

                _pos = p;
                if (p == _span.Length)
                {
                    break;
                }

                b = _span[p];
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
                    if (width == 0)
                    {
                        return NeedMore();
                    }

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
                if (width2 == 0)
                {
                    return NeedMore();
                }

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
            // The declaration Utf8XmlWriter writes, and most other producers too, is recognized with one
            // comparison. It declares version 1.0, which is what the reader starts with, and UTF-8. Any other
            // spelling takes the general path below.
            ReadOnlySpan<byte> usual = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"u8;
            if (_xmlDeclarationAllowed && _span[_pos..].StartsWith(usual))
            {
                _nameStart = _pos + 2;
                _nameLength = 3;
                _prefixLength = 0;
                _valueStart = _pos + 6;
                _valueLength = usual.Length - 8;
                _pos += usual.Length;
                _tokenType = XmlTokenType.XmlDeclaration;
                _xmlDeclarationAllowed = false;
                return true;
            }

            _pos += 2;
            if (!ReadName(out var targetPrefixLength, out var targetLength))
            {
                if (_incomplete)
                {
                    return false;
                }

                throw Error("Invalid processing instruction.");
            }

            if (targetPrefixLength > 0)
            {
                throw Error("A processing instruction target must not contain a colon.");
            }

            var targetStart = _pos - targetLength;
            var target = _span.Slice(targetStart, targetLength);
            if (_pos + 1 < _span.Length
                && !XmlChars.IsWhiteSpace(_span[_pos])
                && !(_span[_pos] is (byte)'?' && _span[_pos + 1] is (byte)'>'))
            {
                throw Error("Whitespace must separate a processing instruction target from its data.");
            }

            SkipWhitespaceBytes();
            var valueStart = _pos;

            // The usual data, an XML declaration above all, is plain ASCII on one line: find its end with one
            // search and check it with another. Anything else is left to the loop, one character at a time.
            var close = _span[_pos..].IndexOf("?>"u8);
            if (close > 0 && !_span.Slice(_pos, close).ContainsAnyExcept(XmlChars.PlainAsciiChars))
            {
                _pos += close;
            }

            while (_pos + 1 < _span.Length)
            {
                if (_span[_pos] is (byte)'?' && _span[_pos + 1] is (byte)'>')
                {
                    _valueStart = valueStart;
                    _valueLength = _pos - valueStart;
                    _pos += 2;
                    if (Ascii.EqualsIgnoreCase(target, "xml"u8))
                    {
                        if (!target.SequenceEqual("xml"u8))
                        {
                            throw Error("Processing instruction targets that match 'xml' in any letter case are reserved.");
                        }

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

                    DecodeValueSpan(valueStart, _valueLength);
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
            _attributeDecodedLength = 0;
            _hasNamespaceAttribute = false;
            _hasXmlSpaceAttribute = false;
            _hasUnresolvedAttribute = false;
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
            if (_attributeCount > 1)
            {
                CheckDuplicateNamespacedAttributes();
            }

            var spaceFlag = 0;
            if (_elementCount > 0)
            {
                var parentSlot = ElementSlot(_elementCount - 1) | ElementHasChild;
                SetElementSlot(_elementCount - 1, parentSlot);
                spaceFlag = parentSlot & ElementPreserveSpace;
            }

            if (_hasXmlSpaceAttribute)
            {
                spaceFlag = ResolveXmlSpace(spaceFlag);
            }

            if (!_isEmptyElement)
            {
                // The element is open from here on, and its start tag is reported at the depth it was read at.
                _isOnOpenStartTag = true;
                var localAdj = prefixLength > 0 ? prefixLength + 1 : 0;
                EnsureElementCapacity();
                if (_elementCount < InlineSlots)
                {
                    _elementNameStartsInline[_elementCount] = nameStart + localAdj;
                    _elementNameLengthsInline[_elementCount] = nameLength - localAdj;
                    _elementPrefixLengthsInline[_elementCount] = prefixLength | spaceFlag;
                }
                else
                {
                    _elementNameStarts[_elementCount] = nameStart + localAdj;
                    _elementNameLengths[_elementCount] = nameLength - localAdj;
                    _elementPrefixLengths[_elementCount] = prefixLength | spaceFlag;
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

            // The value stays in the input until a reference or a line end has to be replaced. From then
            // on it is built in the decode buffer: rawStart is the first input byte not yet copied there,
            // and decodedWritten is how much of the buffer is filled.
            var rawStart = start;
            var decodedWritten = -1;
            var hasNonWs = false;
            var stops = _version is XmlVersion.Version11 ? XmlChars.TextStops11 : XmlChars.TextStops10;
            Span<byte> entityScratch = stackalloc byte[8];

            // Most text is short: a number, a word, the indentation between two tags. A vectorized search
            // costs more to set up than those few bytes cost to read, so the first bytes are read one at a
            // time. Only bytes that mean the same in both XML versions are taken here: printable ASCII
            // other than '<', '&' and '>', the blank, the tab, and the line feed. Anything else, and
            // anything longer, is left to the loop below.
            var head = _pos;
            var headEnd = Math.Min(_span.Length, head + ShortTextBytes);
            while (head < headEnd)
            {
                var b = _span[head];
                if (b is > (byte)' ' and < 0x7F and not (byte)'<' and not (byte)'&' and not (byte)'>')
                {
                    hasNonWs = true;
                    head++;
                }
                else if (b is (byte)' ' or (byte)'\t')
                {
                    head++;
                }
                else if (b is (byte)'\n')
                {
                    head++;
                    _lineNumber++;
                    _lineStart = head;
                }
                else
                {
                    break;
                }
            }

            _pos = head;
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

                    // What the run check left behind starts with a byte that is neither plain text nor
                    // whitespace: a control character, DEL, or a broken multi-byte sequence.
                    while (_pos < _span.Length)
                    {
                        hasNonWs = true;
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
                    if (decodedWritten < 0 && !BeginTextDecode(start, ref decodedWritten))
                    {
                        return NeedMore();
                    }

                    var referenceStart = _pos;
                    if (!DecodeEntity(entityScratch, out var entityWritten))
                    {
                        return NeedMore();
                    }

                    AppendDecodedText(rawStart, referenceStart, entityScratch[..entityWritten], ref decodedWritten);
                    rawStart = _pos;
                    continue;
                }

                var lineEnd = LineEndToReplace(_pos, _span.Length);
                if (lineEnd > 0)
                {
                    // CR, CR LF and, in XML 1.1, CR NEL, NEL and LS all become one LF (section 2.11).
                    if (decodedWritten < 0 && !BeginTextDecode(start, ref decodedWritten))
                    {
                        return NeedMore();
                    }

                    AppendDecodedText(rawStart, _pos, "\n"u8, ref decodedWritten);
                    _pos += lineEnd;
                    _lineNumber++;
                    _lineStart = _pos;
                    rawStart = _pos;
                    continue;
                }

                if (_span[_pos] is (byte)'>')
                {
                    // "]]>" must not appear in character data (XML 1.0 and 1.1 section 2.4).
                    if (_pos - start >= 2 && _span[_pos - 1] is (byte)']' && _span[_pos - 2] is (byte)']')
                    {
                        throw Error("']]>' is not allowed in character data.");
                    }

                    hasNonWs = true;
                    _pos++;
                    continue;
                }

                if (!XmlChars.IsWhiteSpace(_span[_pos]))
                {
                    hasNonWs = true;
                }

                // What is left is LF, which is already the normalized line end, or a character the run
                // check would not pass.
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
                throw TextTooLong();
            }

            if (decodedWritten >= 0)
            {
                AppendDecodedText(rawStart, _pos, default, ref decodedWritten);
                _decodedLength = decodedWritten;
                _valueIsEscaped = true;
            }
            else
            {
                _valueIsEscaped = false;
            }

            if (!hasNonWs)
            {
                return HandleWhitespaceOnlyText();
            }

            _nameLength = 0;
            _prefixLength = 0;
            SetElementSlot(_elementCount - 1, ElementSlot(_elementCount - 1) | ElementHasText);
            _tokenType = XmlTokenType.Text;
            return true;
        }

        /// <summary>
        /// Reports where the reader is, in the three numbers a state snapshot keeps about position.
        /// </summary>
        /// <param name="documentBytesConsumed">The document offset after the current token.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <param name="lineStart">The line start.</param>
        /// <remarks>
        /// Pass them to <c>XmlReaderStateSnapshot.At</c> on a snapshot captured in the same scope to get the
        /// state to resume from here without capturing a new snapshot.
        /// </remarks>
        internal readonly void GetResumePoint(out long documentBytesConsumed, out long lineNumber, out long lineStart)
        {
            documentBytesConsumed = _documentBytesConsumedBase + _pos;
            lineNumber = _lineNumber;
            lineStart = _lineStart;
        }

        /// <summary>
        /// Reads from a start tag to its matching end tag.
        /// </summary>
        /// <returns><see langword="false"/> when the block ended first.</returns>
        internal bool SkipChildren()
        {
            var target = _depth;
            while (Read())
            {
                if (_tokenType == XmlTokenType.EndElement && _depth < target)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Skips the current element when the block may end before the element does, and puts the reader back
        /// on the start tag if it does.
        /// </summary>
        /// <returns><see langword="false"/> when more data is required.</returns>
        /// <remarks>
        /// Kept out of <see cref="TrySkip"/> on purpose: the copy of the reader is a large local, and a method
        /// that declares one clears it on every call, including the final-block calls that never use it.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal bool TrySkipPartial()
        {
            var restore = this;
            if (SkipChildren())
            {
                return true;
            }

            this = restore;
            return false;
        }

        /// <summary>
        /// Consumes a UTF-8 byte order mark when the buffer is the start of the document and begins with one.
        /// </summary>
        /// <remarks>
        /// Only the first three bytes of a document can be a byte order mark. In a later buffer the same
        /// bytes are the character U+FEFF and are content.
        /// </remarks>
        internal void SkipBomIfAtStart()
        {
            if (_documentBytesConsumedBase == 0 && _span.Length >= 3 && _span[0] is 0xEF && _span[1] is 0xBB && _span[2] is 0xBF)
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
            while (_pos < _span.Length)
            {
                var b = _span[_pos];
                if (b is (byte)' ' or (byte)'\t')
                {
                    _pos++;
                }
                else if (b is (byte)'\n' or (byte)'\r')
                {
                    // Line ends move the line counter.
                    AdvanceChar();
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Matches an end tag against the current element's local name without a full name scan.
        /// </summary>
        /// <param name="nameStart">When this method returns true, the byte offset of the qualified name.</param>
        /// <param name="nameLength">When this method returns true, the qualified-name byte length.</param>
        /// <param name="prefixLength">When this method returns true, the prefix byte length, or 0.</param>
        /// <returns><see langword="true"/> if the buffer holds that local name followed by a name terminator.</returns>
        /// <remarks>
        /// Handles <c>&lt;/local&gt;</c> and <c>&lt;/prefix:local&gt;</c>. Does not advance <c>_pos</c> on failure. The caller
        /// has checked that an element is open.
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
        /// Creates the error for a text node longer than <see cref="XmlReaderOptions.MaxTextByteCount"/>.
        /// </summary>
        /// <returns>The exception to throw.</returns>
        internal readonly XmlException TextTooLong()
        {
            return Error($"{nameof(XmlReaderOptions.MaxTextByteCount)} exceeded ({_maxTextBytes}).");
        }

        /// <summary>
        /// Reads the rest of the open element in one step when it is a run of plain text followed at once by the
        /// element's own end tag, written with the same bytes as its start tag.
        /// </summary>
        /// <param name="content">When this method returns <see langword="true"/>, the text, as a slice of the input.</param>
        /// <returns>
        /// <see langword="true"/> when the text and the end tag were read; the reader is then on the end tag, as it
        /// would be after two calls to <see cref="Read"/>. <see langword="false"/> when the content is anything
        /// else, or is not all in the current block; nothing has been read and the caller uses <see cref="Read"/>.
        /// </returns>
        /// <remarks>
        /// This is the shape of nearly every element that holds one value, <c>&lt;Id&gt;42&lt;/Id&gt;</c>, and reading
        /// it as two tokens spends more on the bookkeeping between them than on the bytes. Plain text here is tab,
        /// blank, and printable ASCII other than <c>&lt;</c>, <c>&amp;</c> and <c>&gt;</c>, with at least one byte
        /// that is not white space among the first few: nothing to decode, no line end to count, and no question
        /// of whether white space is significant. The caller must not be on an empty element.
        /// </remarks>
        internal bool TryReadLeafContent(out ReadOnlySpan<byte> content)
        {
            content = default;
            var start = _pos;
            var end = start;
            var headEnd = Math.Min(_span.Length, start + ShortTextBytes);
            var hasNonWs = false;
            while (end < headEnd)
            {
                var b = _span[end];
                if (b is > (byte)' ' and < 0x7F and not (byte)'<' and not (byte)'&' and not (byte)'>')
                {
                    hasNonWs = true;
                }
                else if (b is not ((byte)' ' or (byte)'\t'))
                {
                    break;
                }

                end++;
            }

            if (!hasNonWs)
            {
                return false;
            }

            if (end == headEnd)
            {
                var more = _span[end..].IndexOfAnyExcept(XmlChars.LeafTextChars);
                if (more < 0)
                {
                    return false;
                }

                end += more;
            }

            if (end + 1 >= _span.Length || _span[end] is not (byte)'<' || _span[end + 1] is not (byte)'/' || end - start > _maxTextBytes)
            {
                return false;
            }

            _pos = end + 2;
            if (!TryMatchStartTagName(out var nameStart, out var nameLength, out var prefixLength) || _span[_pos] is not (byte)'>')
            {
                _pos = start;
                return false;
            }

            _pos++;
            _inAttribute = false;
            _attributeIndex = -1;
            _decodedLength = 0;
            _valueIsEscaped = false;
            _valueLength = 0;
            _tokenStartIndex = _documentBytesConsumedBase + end;
            _tokenLineNumber = _lineNumber;
            _tokenLineStart = _lineStart;
            RestoreCurrentNamespaceFromElement(_elementCount - 1);
            PopNamespaceScope(_depth);
            _elementCount--;
            _depth--;
            _nameStart = nameStart;
            _nameLength = nameLength;
            _prefixLength = prefixLength;
            _tokenType = XmlTokenType.EndElement;
            _attributeCount = 0;
            content = _span.Slice(start, end - start);
            return true;
        }

        /// <summary>
        /// Matches an end tag against the bytes of the current element's start-tag name, prefix included.
        /// </summary>
        /// <param name="nameStart">The byte offset of the end-tag name.</param>
        /// <param name="nameLength">When this method returns true, the qualified-name byte length.</param>
        /// <param name="prefixLength">When this method returns true, the prefix byte length, or 0.</param>
        /// <returns><see langword="true"/> if the end tag repeats the start tag's qualified name exactly.</returns>
        /// <remarks>
        /// This is the BCL interned-name compare done on bytes: one comparison settles the local name and
        /// the prefix together, so a matching end tag needs no name scan and no namespace lookup.
        /// It declines (without advancing) for elements restored from a resume snapshot, for a different
        /// prefix bound to the same namespace, and for anything else <see cref="TryMatchEndName"/> must judge.
        /// </remarks>
        internal bool TryMatchStartTagName(out int nameStart, out int nameLength, out int prefixLength)
        {
            nameStart = _pos;
            nameLength = 0;
            prefixLength = 0;
            var index = _elementCount - 1;
            if (index < _resumeNameCount)
            {
                return false;
            }

            int localStart;
            int localLength;
            int startPrefixLength;
            if (index < InlineSlots)
            {
                localStart = _elementNameStartsInline[index];
                localLength = _elementNameLengthsInline[index];
                startPrefixLength = _elementPrefixLengthsInline[index] & ElementPrefixLengthMask;
            }
            else
            {
                localStart = _elementNameStarts[index];
                localLength = _elementNameLengths[index];
                startPrefixLength = _elementPrefixLengths[index] & ElementPrefixLengthMask;
            }

            var prefixAdj = startPrefixLength > 0 ? startPrefixLength + 1 : 0;
            var length = localLength + prefixAdj;
            var after = _pos + length;
            if (after >= _span.Length)
            {
                return false;
            }

            var terminator = _span[after];
            if (terminator >= 0x80 || XmlChars.IsAsciiNcNameChar(terminator) || terminator is (byte)':')
            {
                return false;
            }

            if (!_span.Slice(_pos, length).SequenceEqual(_span.Slice(localStart - prefixAdj, length)))
            {
                return false;
            }

            nameLength = length;
            prefixLength = startPrefixLength;
            _pos = after;
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
                var digit = b <= (byte)'9' ? b - (byte)'0' : (b | 0x20) - (byte)'a' + 10;
                if (value > (0x10FFFF - digit) / (hex ? 16 : 10))
                {
                    return false;
                }

                value = (value * (hex ? 16 : 10)) + digit;
            }

            return true;
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
                if (_depth == 0)
                {
                    throw Error("A CDATA section is not allowed outside the root element.");
                }

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
