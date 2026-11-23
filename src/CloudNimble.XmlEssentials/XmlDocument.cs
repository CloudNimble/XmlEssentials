using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using CloudNimble.Text.Xml.Nodes;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// An immutable pooled parse of a UTF-8 XML document, analogous to <c>JsonDocument</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// using var document = XmlDocument.Parse("&lt;order id=\"7\"&gt;&lt;qty&gt;3&lt;/qty&gt;&lt;/order&gt;"u8);
    /// var id = document.RootElement.GetAttribute("id").GetInt32();
    /// var qty = document.RootElement.GetChild("qty").GetInt32();
    /// </code>
    /// </example>
    /// <remarks>
    /// Dispose returns rented buffers. Use after dispose throws <see cref="ObjectDisposedException"/>.
    /// Prolog and epilog comments or processing instructions are preserved for <see cref="WriteTo"/>
    /// and are not children of <see cref="RootElement"/>.
    /// The document owns a pooled copy of the UTF-8 payload. Names and unescaped values are offsets
    /// into that copy; only entity-decoded values and interned namespace URIs are copied a second time.
    /// </remarks>
    public sealed class XmlDocument : IDisposable
    {

        #region Fields

        private XmlDeclarationInfo? _declaration;
        private bool _disposed;
        private byte[] _extra;
        private int _extraLength;
        private int _nsCacheLength;
        private int _nsCacheStart;
        private int _rootIndex;
        private int _rowCount;
        private XmlDbRow[] _rows;
        private byte[] _source;
        private int _sourceLength;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the XML declaration captured at parse time, or <see langword="null"/> when omitted.
        /// </summary>
        public XmlDeclarationInfo? Declaration
        {
            get
            {
                ThrowIfDisposed();
                return _declaration;
            }
        }

        /// <summary>
        /// Gets the document element.
        /// </summary>
        public XmlElement RootElement
        {
            get
            {
                ThrowIfDisposed();
                if (_rootIndex < 0)
                {
                    throw new XmlException("The document has no root element.");
                }

                return new XmlElement(this, _rootIndex);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an empty document database that copies every captured slice.
        /// </summary>
        internal XmlDocument() : this([], 0)
        {
        }

        /// <summary>
        /// Creates an empty document database over an owned UTF-8 payload.
        /// </summary>
        /// <param name="source">A buffer rented from <see cref="ArrayPool{T}.Shared"/>, or an empty array.</param>
        /// <param name="sourceLength">The number of payload bytes in <paramref name="source"/>.</param>
        /// <remarks>
        /// The document takes ownership of <paramref name="source"/> and returns it on <see cref="Dispose"/>.
        /// Rows are pre-sized on the assumption of one node per 12 payload bytes (the STJ <c>MetadataDb</c> estimate).
        /// </remarks>
        internal XmlDocument(byte[] source, int sourceLength)
        {
            _source = source;
            _sourceLength = sourceLength;
            _extra = [];
            _rows = ArrayPool<XmlDbRow>.Shared.Rent(Math.Max(16, sourceLength / 12));
            _rootIndex = -1;
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

            _disposed = true;
            ReturnBuffer(_source, _sourceLength);
            ReturnBuffer(_extra, _extraLength);
            ArrayPool<XmlDbRow>.Shared.Return(_rows);
            _source = [];
            _extra = [];
            _rows = [];
            _sourceLength = 0;
            _extraLength = 0;
            _rowCount = 0;
        }

        /// <summary>
        /// Parses <paramref name="xml"/> as UTF-16 converted to UTF-8.
        /// </summary>
        /// <param name="xml">The XML text.</param>
        /// <param name="options">Parse options.</param>
        /// <returns>A disposable document.</returns>
        public static XmlDocument Parse(string xml, XmlDocumentOptions options = default)
        {
            ArgumentNullException.ThrowIfNull(xml);
            return Parse(xml.AsSpan(), options);
        }

        /// <summary>
        /// Parses UTF-16 XML.
        /// </summary>
        /// <param name="xml">The XML text.</param>
        /// <param name="options">Parse options.</param>
        /// <returns>A disposable document.</returns>
        public static XmlDocument Parse(ReadOnlySpan<char> xml, XmlDocumentOptions options = default)
        {
            var byteCount = Encoding.UTF8.GetByteCount(xml);
            ThrowIfDocumentTooLarge(byteCount, options.MaxDocumentByteCount);
            var rented = ArrayPool<byte>.Shared.Rent(byteCount);
            var written = Encoding.UTF8.GetBytes(xml, rented);
            return ParseOwned(rented, written, options);
        }

        /// <summary>
        /// Parses UTF-8 XML.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 XML.</param>
        /// <param name="options">Parse options.</param>
        /// <returns>A disposable document.</returns>
        public static XmlDocument Parse(ReadOnlySpan<byte> utf8Xml, XmlDocumentOptions options = default)
        {
            ThrowIfDocumentTooLarge(utf8Xml.Length, options.MaxDocumentByteCount);
            var rented = ArrayPool<byte>.Shared.Rent(utf8Xml.Length);
            utf8Xml.CopyTo(rented);
            return ParseOwned(rented, utf8Xml.Length, options);
        }

        /// <summary>
        /// Parses a UTF-8 sequence.
        /// </summary>
        /// <param name="utf8Xml">The sequence.</param>
        /// <param name="options">Parse options.</param>
        /// <returns>A disposable document.</returns>
        public static XmlDocument Parse(ReadOnlySequence<byte> utf8Xml, XmlDocumentOptions options = default)
        {
            var length = checked((int)utf8Xml.Length);
            ThrowIfDocumentTooLarge(length, options.MaxDocumentByteCount);
            var rented = ArrayPool<byte>.Shared.Rent(length);
            utf8Xml.CopyTo(rented);
            return ParseOwned(rented, length, options);
        }

        /// <summary>
        /// Reads a UTF-8 stream to its end and parses it.
        /// </summary>
        /// <param name="utf8Xml">The stream.</param>
        /// <param name="options">Parse options.</param>
        /// <returns>A disposable document.</returns>
        public static XmlDocument Parse(Stream utf8Xml, XmlDocumentOptions options = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            var rented = XmlStreamBuffer.ReadToEnd(utf8Xml, options.MaxDocumentByteCount, out var length);
            return ParseOwned(rented, length, options);
        }

        /// <summary>
        /// Parses the current element (and its descendants) from <paramref name="reader"/> as a document.
        /// </summary>
        /// <param name="reader">A reader positioned at or before a start tag.</param>
        /// <returns>A disposable document whose root is that element.</returns>
        public static XmlDocument ParseValue(ref Utf8XmlReader reader)
        {
            var document = new XmlDocument();
            try
            {
                document.FillFromReader(ref reader, requireDocumentRoot: false);
                return document;
            }
            catch
            {
                document.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Attempts to parse UTF-8 XML.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 XML.</param>
        /// <param name="options">Parse options.</param>
        /// <param name="document">When this method returns true, the parsed document.</param>
        /// <returns><see langword="true"/> if the payload was well-formed.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Xml, XmlDocumentOptions options, out XmlDocument? document)
        {
            try
            {
                document = Parse(utf8Xml, options);
                return true;
            }
            catch (XmlException)
            {
                document = null;
                return false;
            }
        }

        /// <summary>
        /// Writes the declaration (when present), prolog, root, and epilog to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The destination writer.</param>
        public void WriteTo(Utf8XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ThrowIfDisposed();
            if (_declaration is { } declaration)
            {
                writer.WriteRawValue(FormatDeclaration(declaration), skipInputValidation: true);
                writer.MarkDeclarationWritten();
            }

            for (var i = 0; i < _rowCount; i = NextSiblingIndex(i))
            {
                WriteRow(i, writer);
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Adds <paramref name="row"/> and returns its index.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns>The row index.</returns>
        internal int AddRow(XmlDbRow row)
        {
            EnsureRows(1);
            var index = _rowCount;
            _rows[index] = row;
            _rowCount = index + 1;
            return index;
        }

        /// <summary>
        /// Appends <paramref name="utf8"/> onto the extra buffer and returns its offset in that buffer.
        /// </summary>
        /// <param name="utf8">The bytes to copy.</param>
        /// <returns>The starting offset within the extra buffer.</returns>
        internal int AppendExtra(ReadOnlySpan<byte> utf8)
        {
            if (_extraLength + utf8.Length > _extra.Length)
            {
                var rented = ArrayPool<byte>.Shared.Rent(Math.Max(Math.Max(_extra.Length * 2, 256), _extraLength + utf8.Length));
                _extra.AsSpan(0, _extraLength).CopyTo(rented);
                ReturnBuffer(_extra, _extraLength);
                _extra = rented;
            }

            utf8.CopyTo(_extra.AsSpan(_extraLength));
            var start = _extraLength;
            _extraLength += utf8.Length;
            return start;
        }

        /// <summary>
        /// Captures the current start tag, including attributes.
        /// </summary>
        /// <param name="reader">The tokenizer on a start tag.</param>
        /// <returns>The element row index.</returns>
        /// <remarks>
        /// Attribute rows directly follow the element row. <c>xsi:nil</c> is detected in the same pass
        /// that copies the attributes, so the tag is walked once.
        /// </remarks>
        internal int CaptureElement(ref Utf8XmlReader reader)
        {
            var attributeCount = reader.AttributeCount;
            var index = _rowCount;
            EnsureRows(attributeCount + 1);
            _rowCount = index + attributeCount + 1;
            var name = reader.CurrentQualifiedName(out var prefixLength);
            var ns = reader.NamespaceUriSpan;
            var isEmpty = reader.IsEmptyElement;
            var isNil = false;
            for (var i = 0; i < attributeCount; i++)
            {
                var attribute = reader.AttributeAt(i);
                var attributeName = reader.AttributeQualifiedName(in attribute);
                var attributeValue = reader.AttributeValue(attribute);
                ReadOnlySpan<byte> attributeNs = [];
                if (attribute.PrefixLength > 0)
                {
                    attributeNs = reader.ResolveAttributeNamespace(attribute);
                    isNil = isNil || IsNilAttribute(attributeName[(attribute.PrefixLength + 1)..], attributeNs, attributeValue);
                }
                else if (attributeName.SequenceEqual(XmlUtf8.XmlnsPrefix))
                {
                    attributeNs = XmlUtf8.Xmlns;
                }

                _rows[index + 1 + i] = new XmlDbRow
                {
                    Kind = XmlValueKind.Attribute,
                    NameStart = Locate(attributeName),
                    NameLength = attributeName.Length,
                    PrefixLength = attribute.PrefixLength,
                    NsStart = LocateNamespace(attributeNs),
                    NsLength = attributeNs.Length,
                    ValueStart = Locate(attributeValue),
                    ValueLength = attributeValue.Length,
                };
            }

            _rows[index] = new XmlDbRow
            {
                Kind = isNil ? XmlValueKind.Nil : isEmpty ? XmlValueKind.EmptyElement : XmlValueKind.Element,
                IsEmpty = isEmpty,
                NameStart = Locate(name),
                NameLength = name.Length,
                PrefixLength = prefixLength,
                NsStart = LocateNamespace(ns),
                NsLength = ns.Length,
                AttributeCount = attributeCount,
                EndIndex = _rowCount,
            };
            return index;
        }

        /// <summary>
        /// Captures the current comment, PI, text, or CDATA token.
        /// </summary>
        /// <param name="reader">The tokenizer.</param>
        /// <returns>The row index.</returns>
        internal int CaptureLeaf(ref Utf8XmlReader reader)
        {
            var kind = reader.TokenType switch
            {
                XmlTokenType.CData => XmlValueKind.CData,
                XmlTokenType.Comment => XmlValueKind.Comment,
                XmlTokenType.ProcessingInstruction => XmlValueKind.ProcessingInstruction,
                _ => XmlValueKind.Text,
            };
            ReadOnlySpan<byte> name = kind is XmlValueKind.ProcessingInstruction ? reader.LocalNameSpan : [];
            var value = reader.ValueSpan;
            return AddRow(new XmlDbRow
            {
                Kind = kind,
                NameStart = Locate(name),
                NameLength = name.Length,
                ValueStart = Locate(value),
                ValueLength = value.Length,
            });
        }

        /// <summary>
        /// Deep-clones row <paramref name="index"/> into a mutable node.
        /// </summary>
        /// <param name="index">The row index.</param>
        /// <returns>The mutable node.</returns>
        internal XmlNode CloneRow(int index)
        {
            ref var row = ref _rows[index];
            switch (row.Kind)
            {
                case XmlValueKind.Text:
                    return new XmlTextNode(Encoding.UTF8.GetString(Value(in row)));
                case XmlValueKind.CData:
                    return new XmlCDataNode(Encoding.UTF8.GetString(Value(in row)));
                case XmlValueKind.Comment:
                    return new XmlCommentNode(Encoding.UTF8.GetString(Value(in row)));
                case XmlValueKind.ProcessingInstruction:
                    return new XmlProcessingInstructionNode(
                        Encoding.UTF8.GetString(LocalName(in row)),
                        Encoding.UTF8.GetString(Value(in row)));
                case XmlValueKind.Attribute:
                    return new XmlAttributeNode(
                        Encoding.UTF8.GetString(LocalName(in row)),
                        Encoding.UTF8.GetString(Value(in row)),
                        Encoding.UTF8.GetString(NamespaceUri(in row)),
                        Encoding.UTF8.GetString(Prefix(in row)));
                default:
                    var element = new XmlElementNode(
                        Encoding.UTF8.GetString(LocalName(in row)),
                        Encoding.UTF8.GetString(NamespaceUri(in row)),
                        Encoding.UTF8.GetString(Prefix(in row)));
                    var end = row.EndIndex;
                    var attributeEnd = index + 1 + row.AttributeCount;
                    for (var a = index + 1; a < attributeEnd; a++)
                    {
                        element.Attributes.Add((XmlAttributeNode)CloneRow(a));
                    }

                    for (var c = attributeEnd; c < end; c = NextSiblingIndex(c))
                    {
                        element.Add(CloneRow(c));
                    }

                    return element;
            }
        }

        /// <summary>
        /// Makes room for <paramref name="count"/> more rows.
        /// </summary>
        /// <param name="count">The number of rows about to be written.</param>
        /// <remarks>
        /// Rows hold offsets and lengths only, never content, so the returned array is not cleared.
        /// </remarks>
        internal void EnsureRows(int count)
        {
            if (_rowCount + count <= _rows.Length)
            {
                return;
            }

            var rented = ArrayPool<XmlDbRow>.Shared.Rent(Math.Max(_rows.Length * 2, _rowCount + count));
            _rows.AsSpan(0, _rowCount).CopyTo(rented);
            ArrayPool<XmlDbRow>.Shared.Return(_rows);
            _rows = rented;
        }

        /// <summary>
        /// Fills this database from <paramref name="reader"/>.
        /// </summary>
        /// <param name="reader">The tokenizer.</param>
        /// <param name="requireDocumentRoot">When true, a single document element is required.</param>
        /// <remarks>
        /// Rows are appended in document order. The open-element stack only remembers which rows still
        /// need their <see cref="XmlDbRow.EndIndex"/> patched when the matching end tag arrives.
        /// </remarks>
        internal void FillFromReader(ref Utf8XmlReader reader, bool requireDocumentRoot)
        {
            Span<int> stack = stackalloc int[XmlLimits.DefaultMaxDepth];
            int[]? rentedStack = null;
            var depth = 0;
            if (reader.TokenType is XmlTokenType.Attribute)
            {
                reader.MoveToElement();
            }

            var started = reader.TokenType is not XmlTokenType.None;
            try
            {
                while (started || reader.Read())
                {
                    started = false;
                    switch (reader.TokenType)
                    {
                        case XmlTokenType.XmlDeclaration:
                            _declaration = ReadDeclaration(ref reader);
                            break;
                        case XmlTokenType.StartElement:
                            if (depth == 0)
                            {
                                if (_rootIndex >= 0)
                                {
                                    throw new XmlException("Multiple root elements are not allowed.");
                                }

                                _rootIndex = _rowCount;
                            }

                            var element = CaptureElement(ref reader);
                            if (!reader.IsEmptyElement)
                            {
                                if (depth == stack.Length)
                                {
                                    var grown = ArrayPool<int>.Shared.Rent(stack.Length * 2);
                                    stack.CopyTo(grown);
                                    ReturnScratch(rentedStack);
                                    rentedStack = grown;
                                    stack = grown;
                                }

                                stack[depth] = element;
                                depth++;
                            }
                            else if (!requireDocumentRoot && depth == 0)
                            {
                                return;
                            }

                            break;
                        case XmlTokenType.EndElement:
                            if (depth == 0)
                            {
                                // ParseValue takes a reader wherever its caller left it, which may be on or
                                // just before the end tag of an element this document never saw start.
                                throw new XmlException("End tag without a matching start tag.");
                            }

                            depth--;
                            _rows[stack[depth]].EndIndex = _rowCount;
                            if (!requireDocumentRoot && depth == 0)
                            {
                                return;
                            }

                            break;
                        default:
                            CaptureLeaf(ref reader);
                            break;
                    }
                }

                if (requireDocumentRoot && _rootIndex < 0)
                {
                    throw new XmlException("The document has no root element.");
                }

            }
            finally
            {
                while (depth > 0)
                {
                    depth--;
                    _rows[stack[depth]].EndIndex = _rowCount;
                }

                ReturnScratch(rentedStack);
            }
        }

        /// <summary>
        /// Formats <paramref name="declaration"/> as an XML declaration.
        /// </summary>
        /// <param name="declaration">The captured declaration.</param>
        /// <returns>The UTF-8 declaration markup.</returns>
        internal static ReadOnlySpan<byte> FormatDeclaration(XmlDeclarationInfo declaration)
        {
            var version11 = declaration.Version is "1.1";
            return declaration.Standalone switch
            {
                true => version11
                    ? "<?xml version=\"1.1\" encoding=\"utf-8\" standalone=\"yes\"?>"u8
                    : "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>"u8,
                false => version11
                    ? "<?xml version=\"1.1\" encoding=\"utf-8\" standalone=\"no\"?>"u8
                    : "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>"u8,
                null => version11
                    ? "<?xml version=\"1.1\" encoding=\"utf-8\"?>"u8
                    : "<?xml version=\"1.0\" encoding=\"utf-8\"?>"u8,
            };
        }

        /// <summary>
        /// Returns whether <paramref name="kind"/> is one of the element kinds.
        /// </summary>
        /// <param name="kind">The row kind.</param>
        /// <returns><see langword="true"/> for element, empty element, and nil rows.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsElementKind(XmlValueKind kind)
        {
            return kind is XmlValueKind.Element or XmlValueKind.EmptyElement or XmlValueKind.Nil;
        }

        /// <summary>
        /// Returns whether an attribute is <c>xsi:nil</c> with a true value.
        /// </summary>
        /// <param name="localName">The attribute local name.</param>
        /// <param name="namespaceUri">The attribute namespace URI.</param>
        /// <param name="value">The decoded attribute value.</param>
        /// <returns><see langword="true"/> for <c>xsi:nil="true"</c> or <c>xsi:nil="1"</c>.</returns>
        internal static bool IsNilAttribute(ReadOnlySpan<byte> localName, ReadOnlySpan<byte> namespaceUri, ReadOnlySpan<byte> value)
        {
            if (!localName.SequenceEqual("nil"u8) || !namespaceUri.SequenceEqual(XmlUtf8.Xsi))
            {
                return false;
            }

            var trimmed = XmlLexical.Collapse(value);
            return trimmed.SequenceEqual("true"u8) || trimmed.SequenceEqual("1"u8);
        }

        /// <summary>
        /// Returns the local name of <paramref name="row"/>.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns>The UTF-8 local name.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<byte> LocalName(in XmlDbRow row)
        {
            var skip = row.PrefixLength > 0 ? row.PrefixLength + 1 : 0;
            return Slice(row.NameStart + skip, row.NameLength - skip);
        }

        /// <summary>
        /// Returns the document offset of <paramref name="utf8"/>, copying it only when it is not part of the owned payload.
        /// </summary>
        /// <param name="utf8">A slice reported by the tokenizer.</param>
        /// <returns>An offset for <see cref="Slice"/>.</returns>
        /// <remarks>
        /// Tokenizer spans over the owned payload are recorded by address, which is the common case.
        /// Entity-decoded values, interned URIs, and everything from <see cref="ParseValue"/> are appended
        /// to the extra buffer, whose offsets start at the payload length.
        /// </remarks>
        internal int Locate(ReadOnlySpan<byte> utf8)
        {
            if (utf8.IsEmpty)
            {
                return 0;
            }

            var offset = Unsafe.ByteOffset(
                ref MemoryMarshal.GetArrayDataReference(_source),
                ref MemoryMarshal.GetReference(utf8));
            if ((nuint)offset < (nuint)_sourceLength)
            {
                return (int)offset;
            }

            return _sourceLength + AppendExtra(utf8);
        }

        /// <summary>
        /// Returns the document offset of a namespace URI, reusing the previous copy when it repeats.
        /// </summary>
        /// <param name="utf8">The namespace URI reported by the tokenizer.</param>
        /// <returns>An offset for <see cref="Slice"/>.</returns>
        /// <remarks>
        /// Sibling elements almost always share a namespace, so one cached copy removes a per-element append.
        /// </remarks>
        internal int LocateNamespace(ReadOnlySpan<byte> utf8)
        {
            if (utf8.IsEmpty)
            {
                return 0;
            }

            if (utf8.Length == _nsCacheLength && utf8.SequenceEqual(Slice(_nsCacheStart, _nsCacheLength)))
            {
                return _nsCacheStart;
            }

            _nsCacheStart = Locate(utf8);
            _nsCacheLength = utf8.Length;
            return _nsCacheStart;
        }

        /// <summary>
        /// Returns the namespace URI of <paramref name="row"/>.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns>The UTF-8 namespace URI, or empty.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<byte> NamespaceUri(in XmlDbRow row)
        {
            return Slice(row.NsStart, row.NsLength);
        }

        /// <summary>
        /// Returns the row that follows <paramref name="index"/> at the same depth.
        /// </summary>
        /// <param name="index">The row index.</param>
        /// <returns>The index after the row, or after its whole subtree for an element.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int NextSiblingIndex(int index)
        {
            ref var row = ref _rows[index];
            return IsElementKind(row.Kind) ? row.EndIndex : index + 1;
        }

        /// <summary>
        /// Parses a rented UTF-8 payload, transferring its ownership to the new document.
        /// </summary>
        /// <param name="rented">A buffer rented from <see cref="ArrayPool{T}.Shared"/>.</param>
        /// <param name="length">The payload length.</param>
        /// <param name="options">Parse options.</param>
        /// <returns>The filled document.</returns>
        internal static XmlDocument ParseOwned(byte[] rented, int length, XmlDocumentOptions options)
        {
            var document = new XmlDocument(rented, length);
            try
            {
                // A rented buffer is usually longer than what was asked for. The reader works in the part
                // after the payload, and what it wrote there is cleared, because the buffer goes back to the
                // shared pool with only the payload cleared.
                var reader = new Utf8XmlReader(rented.AsSpan(0, length), rented.AsSpan(length), sequenceCopy: null, sequence: default, hasSequence: false, isFinalBlock: true, new XmlReaderState(options.ToReaderOptions()));
                document.FillFromReader(ref reader, requireDocumentRoot: true);
                if (reader.ScratchBytesUsed != 0)
                {
                    rented.AsSpan(length, reader.ScratchBytesUsed).Clear();
                }

                return document;
            }
            catch
            {
                // The reader is gone by now, so everything after the payload is cleared.
                rented.AsSpan(length).Clear();
                document.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Returns the prefix of <paramref name="row"/>.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns>The UTF-8 prefix, or empty.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<byte> Prefix(in XmlDbRow row)
        {
            return Slice(row.NameStart, row.PrefixLength);
        }

        /// <summary>
        /// Reads declaration fields from the current token.
        /// </summary>
        /// <param name="reader">The tokenizer on an XML declaration it has already validated.</param>
        /// <returns>The captured declaration.</returns>
        internal static XmlDeclarationInfo ReadDeclaration(ref Utf8XmlReader reader)
        {
            XmlDeclarationSyntax.TryParse(reader.ValueSpan, out var version, out _, out var standalone);
            return new XmlDeclarationInfo(version is XmlVersion.Version11 ? "1.1" : "1.0", "utf-8", standalone);
        }

        /// <summary>
        /// Clears the used part of a pooled content buffer and returns it.
        /// </summary>
        /// <param name="buffer">The rented buffer, or an empty array.</param>
        /// <param name="used">The number of bytes holding document content.</param>
        /// <remarks>
        /// Content buffers hold caller data, so they are cleared before they go back to the shared pool
        /// (STJ <c>JsonDocument.Dispose</c>).
        /// </remarks>
        internal static void ReturnBuffer(byte[] buffer, int used)
        {
            if (buffer.Length == 0)
            {
                return;
            }

            buffer.AsSpan(0, used).Clear();
            ArrayPool<byte>.Shared.Return(buffer);
        }

        /// <summary>
        /// Returns a scratch array to the shared pool when one was rented.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="rented">The rented array, or <see langword="null"/>.</param>
        internal static void ReturnScratch<T>(T[]? rented)
        {
            if (rented is not null)
            {
                ArrayPool<T>.Shared.Return(rented, clearArray: true);
            }
        }

        /// <summary>
        /// Returns the row at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The row index.</param>
        /// <returns>The row.</returns>
        /// <remarks>
        /// Does not check for disposal. Public entry points call <see cref="ThrowIfDisposed"/> once.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref XmlDbRow Row(int index)
        {
            return ref _rows[index];
        }

        /// <summary>
        /// Returns <paramref name="length"/> bytes at document offset <paramref name="start"/>.
        /// </summary>
        /// <param name="start">An offset below the payload length for the payload, otherwise into the extra buffer.</param>
        /// <param name="length">The length.</param>
        /// <returns>The UTF-8 bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<byte> Slice(int start, int length)
        {
            return start < _sourceLength
                ? _source.AsSpan(start, length)
                : _extra.AsSpan(start - _sourceLength, length);
        }

        /// <summary>
        /// Throws when this instance has been disposed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        /// <summary>
        /// Throws when <paramref name="length"/> exceeds a non-zero document cap.
        /// </summary>
        /// <param name="length">The payload length.</param>
        /// <param name="maxDocumentByteCount">The configured cap.</param>
        internal static void ThrowIfDocumentTooLarge(int length, int maxDocumentByteCount)
        {
            if (maxDocumentByteCount > 0 && length > maxDocumentByteCount)
            {
                throw new XmlException($"{nameof(XmlDocumentOptions.MaxDocumentByteCount)} exceeded ({maxDocumentByteCount}).");
            }
        }

        /// <summary>
        /// Compares UTF-8 <paramref name="utf8"/> to UTF-16 <paramref name="text"/> without allocating.
        /// </summary>
        /// <param name="utf8">Well-formed UTF-8 from the document.</param>
        /// <param name="text">The caller's text.</param>
        /// <returns><see langword="true"/> if both encode the same string.</returns>
        /// <remarks>
        /// A non-ASCII character always takes more UTF-8 bytes than UTF-16 code units. Equal lengths can
        /// therefore only match as pure ASCII, and a shorter UTF-8 form can never match, so the name
        /// lookups on <see cref="XmlElement"/> rarely transcode.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Utf8Equals(ReadOnlySpan<byte> utf8, ReadOnlySpan<char> text)
        {
            if (utf8.Length == text.Length)
            {
                return Ascii.Equals(utf8, text);
            }

            return utf8.Length > text.Length && Utf8EqualsTranscoded(utf8, text);
        }

        /// <summary>
        /// Compares UTF-8 <paramref name="utf8"/> to UTF-16 <paramref name="text"/> by transcoding the text.
        /// </summary>
        /// <param name="utf8">Well-formed UTF-8 from the document.</param>
        /// <param name="text">The caller's text.</param>
        /// <returns><see langword="true"/> if both encode the same string.</returns>
        /// <remarks>
        /// Ill-formed UTF-16 never matches; it is not replaced with U+FFFD.
        /// </remarks>
        internal static bool Utf8EqualsTranscoded(ReadOnlySpan<byte> utf8, ReadOnlySpan<char> text)
        {
            byte[]? rented = null;
            Span<byte> scratch = utf8.Length <= 256
                ? stackalloc byte[256]
                : (rented = ArrayPool<byte>.Shared.Rent(utf8.Length));
            var status = Utf8.FromUtf16(text, scratch[..utf8.Length], out _, out var written, replaceInvalidSequences: false);
            var equal = status is OperationStatus.Done && scratch[..written].SequenceEqual(utf8);
            ReturnScratch(rented);
            return equal;
        }

        /// <summary>
        /// Returns the value of an attribute or leaf <paramref name="row"/>.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <returns>The UTF-8 value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<byte> Value(in XmlDbRow row)
        {
            return Slice(row.ValueStart, row.ValueLength);
        }

        /// <summary>
        /// Writes element row <paramref name="index"/>, its attributes, and its children.
        /// </summary>
        /// <param name="index">The element row index.</param>
        /// <param name="writer">The destination.</param>
        /// <remarks>
        /// Namespace declarations are skipped; the writer re-declares each namespace from the names it is given.
        /// </remarks>
        internal void WriteElement(int index, Utf8XmlWriter writer)
        {
            ref var row = ref _rows[index];
            var end = row.EndIndex;
            var attributeEnd = index + 1 + row.AttributeCount;
            if (row.AttributeCount == 0 && end - attributeEnd == 1 && _rows[attributeEnd].Kind is XmlValueKind.Text)
            {
                writer.WriteCompleteElementUtf8(LocalName(in row), Prefix(in row), NamespaceUri(in row), Value(in _rows[attributeEnd]));
                return;
            }

            var elementPrefix = Prefix(in row);
            var elementNs = NamespaceUri(in row);
            writer.WriteStartElementDeferred(LocalName(in row), elementPrefix);
            var declared = false;
            for (var a = index + 1; a < attributeEnd; a++)
            {
                ref var attribute = ref _rows[a];
                if (!IsNamespaceDeclaration(in attribute, out var declaredPrefix))
                {
                    continue;
                }

                if (declaredPrefix.SequenceEqual(elementPrefix))
                {
                    declared = true;
                }

                writer.RegisterNamespace(declaredPrefix, Value(in attribute));
            }

            if (!declared)
            {
                writer.EnsureElementNamespace(elementPrefix, elementNs);
            }

            for (var a = index + 1; a < attributeEnd; a++)
            {
                ref var attribute = ref _rows[a];
                if (IsNamespaceDeclaration(in attribute, out var declaredPrefix))
                {
                    writer.WriteRegisteredNamespace(declaredPrefix, Value(in attribute));
                    continue;
                }

                writer.WriteStartAttributeUtf8(LocalName(in attribute), Prefix(in attribute), NamespaceUri(in attribute));
                writer.WriteStringValue(Value(in attribute));
                writer.WriteEndAttribute();
            }

            for (var c = attributeEnd; c < end; c = NextSiblingIndex(c))
            {
                WriteRow(c, writer);
            }

            if (row.IsEmpty)
            {
                writer.WriteEndElement();
            }
            else
            {
                writer.WriteFullEndElement();
            }
        }

        /// <summary>
        /// Returns whether an attribute row is an <c>xmlns</c> or <c>xmlns:prefix</c> declaration.
        /// </summary>
        /// <param name="attribute">The attribute row.</param>
        /// <param name="declaredPrefix">When this method returns true, the declared prefix, or empty for the default namespace.</param>
        /// <returns><see langword="true"/> for a namespace declaration. An attribute such as <c>p:xmlns</c> is not one.</returns>
        internal bool IsNamespaceDeclaration(in XmlDbRow attribute, out ReadOnlySpan<byte> declaredPrefix)
        {
            if (attribute.PrefixLength == 0)
            {
                declaredPrefix = [];
                return attribute.NameLength == 5 && Slice(attribute.NameStart, 5).SequenceEqual(XmlUtf8.XmlnsPrefix);
            }

            declaredPrefix = LocalName(in attribute);
            return attribute.PrefixLength == 5 && Slice(attribute.NameStart, 5).SequenceEqual(XmlUtf8.XmlnsPrefix);
        }

        /// <summary>
        /// Writes row <paramref name="index"/> to <paramref name="writer"/>.
        /// </summary>
        /// <param name="index">The row index.</param>
        /// <param name="writer">The destination.</param>
        internal void WriteRow(int index, Utf8XmlWriter writer)
        {
            ref var row = ref _rows[index];
            switch (row.Kind)
            {
                case XmlValueKind.Element:
                case XmlValueKind.EmptyElement:
                case XmlValueKind.Nil:
                    WriteElement(index, writer);
                    break;
                case XmlValueKind.Text:
                    writer.WriteStringValue(Value(in row));
                    break;
                case XmlValueKind.CData:
                    writer.WriteCData(Value(in row));
                    break;
                case XmlValueKind.Comment:
                    writer.WriteComment(Value(in row));
                    break;
                case XmlValueKind.ProcessingInstruction:
                    writer.WriteProcessingInstructionUtf8(LocalName(in row), Value(in row));
                    break;
            }
        }

        #endregion

    }

}
