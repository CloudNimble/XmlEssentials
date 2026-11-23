using System;
using System.Buffers;
using System.Text;
using CloudNimble.Text.Xml.Nodes;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// A struct cursor into an <see cref="XmlDocument"/>, analogous to <c>JsonElement</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// using var document = XmlDocument.Parse("&lt;catalog&gt;&lt;item id=\"1\"&gt;&lt;qty&gt;3&lt;/qty&gt;&lt;/item&gt;&lt;/catalog&gt;"u8);
    /// foreach (var item in document.RootElement.EnumerateChildren())
    /// {
    ///     var id = item.GetAttribute("id").GetInt32();
    ///     var qty = item.GetChild("qty").GetInt32();
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Name lookups, enumeration, the numeric and Boolean getters, and <see cref="WriteTo"/> do not allocate.
    /// Members that return a <see cref="string"/>, a <see cref="T:byte[]"/>, or an <see cref="XmlNode"/>
    /// allocate only that result.
    /// </remarks>
    public readonly struct XmlElement : IEquatable<XmlElement>
    {

        #region Fields

        private readonly XmlDocument? _document;
        private readonly int _index;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the number of attributes on this element.
        /// </summary>
        public int AttributeCount
        {
            get
            {
                ref var row = ref EnsureDocument().Row(_index);
                return XmlDocument.IsElementKind(row.Kind) ? row.AttributeCount : 0;
            }
        }

        /// <summary>
        /// Gets whether this element is empty (<c>&lt;e/&gt;</c>).
        /// </summary>
        public bool IsEmptyElement => EnsureDocument().Row(_index).IsEmpty;

        /// <summary>
        /// Gets whether this element carries <c>xsi:nil="true"</c>.
        /// </summary>
        public bool IsNil => EnsureDocument().Row(_index).Kind is XmlValueKind.Nil;

        /// <summary>
        /// Gets the local name.
        /// </summary>
        public string LocalName
        {
            get
            {
                var document = EnsureDocument();
                return Encoding.UTF8.GetString(document.LocalName(in document.Row(_index)));
            }
        }

        /// <summary>
        /// Gets the namespace URI, or empty.
        /// </summary>
        public string NamespaceUri
        {
            get
            {
                var document = EnsureDocument();
                return Encoding.UTF8.GetString(document.NamespaceUri(in document.Row(_index)));
            }
        }

        /// <summary>
        /// Gets the prefix, or empty.
        /// </summary>
        public string Prefix
        {
            get
            {
                var document = EnsureDocument();
                return Encoding.UTF8.GetString(document.Prefix(in document.Row(_index)));
            }
        }

        /// <summary>
        /// Gets the token type corresponding to this cursor.
        /// </summary>
        public XmlTokenType TokenType
        {
            get
            {
                return EnsureDocument().Row(_index).Kind switch
                {
                    XmlValueKind.Element or XmlValueKind.EmptyElement or XmlValueKind.Nil => XmlTokenType.StartElement,
                    XmlValueKind.Attribute => XmlTokenType.Attribute,
                    XmlValueKind.Text => XmlTokenType.Text,
                    XmlValueKind.CData => XmlTokenType.CData,
                    XmlValueKind.Comment => XmlTokenType.Comment,
                    XmlValueKind.ProcessingInstruction => XmlTokenType.ProcessingInstruction,
                    _ => XmlTokenType.None,
                };
            }
        }

        /// <summary>
        /// Gets the value kind of this cursor.
        /// </summary>
        public XmlValueKind ValueKind
        {
            get
            {
                if (_document is null)
                {
                    return XmlValueKind.Undefined;
                }

                _document.ThrowIfDisposed();
                return _document.Row(_index).Kind;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a cursor into <paramref name="document"/> at <paramref name="index"/>.
        /// </summary>
        /// <param name="document">The owning document.</param>
        /// <param name="index">The row index.</param>
        internal XmlElement(XmlDocument document, int index)
        {
            _document = document;
            _index = index;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Deep-clones this cursor into a mutable node.
        /// </summary>
        /// <returns>The mutable clone.</returns>
        public XmlNode CloneNode()
        {
            return EnsureDocument().CloneRow(_index);
        }

        /// <inheritdoc />
        public bool Equals(XmlElement other)
        {
            return ReferenceEquals(_document, other._document) && _index == other._index;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is XmlElement other && Equals(other);
        }

        /// <summary>
        /// Enumerates attributes on this element.
        /// </summary>
        /// <returns>An attribute enumerator.</returns>
        public AttributeEnumerator EnumerateAttributes()
        {
            var document = EnsureDocument();
            ref var row = ref document.Row(_index);
            var count = XmlDocument.IsElementKind(row.Kind) ? row.AttributeCount : 0;
            return new AttributeEnumerator(document, _index + 1, _index + 1 + count);
        }

        /// <summary>
        /// Enumerates child nodes of this element.
        /// </summary>
        /// <returns>A child enumerator.</returns>
        public Enumerator EnumerateChildren()
        {
            var document = EnsureDocument();
            ChildRange(document, out var first, out var end);
            return new Enumerator(document, first, end);
        }

        /// <summary>
        /// Returns the named attribute.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <returns>The attribute cursor.</returns>
        public XmlElement GetAttribute(string localName)
        {
            if (!TryGetAttribute(localName, out var value))
            {
                throw new XmlException($"Attribute '{localName}' was not found.");
            }

            return value;
        }

        /// <summary>
        /// Returns the named attribute in <paramref name="namespaceUri"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <returns>The attribute cursor.</returns>
        public XmlElement GetAttribute(string localName, string namespaceUri)
        {
            ArgumentNullException.ThrowIfNull(localName);
            ArgumentNullException.ThrowIfNull(namespaceUri);
            if (!TryFindAttribute(localName, namespaceUri, out var value))
            {
                throw new XmlException($"Attribute '{localName}' was not found.");
            }

            return value;
        }

        /// <summary>
        /// Returns the current value as a boolean.
        /// </summary>
        /// <returns>The parsed value.</returns>
        public bool GetBoolean()
        {
            var utf8 = TypedValueUtf8(out var rented);
            var parsed = XmlLexical.TryParseBoolean(utf8, out var value);
            XmlDocument.ReturnScratch(rented);
            if (!parsed)
            {
                throw new XmlException("The value is not a boolean.");
            }

            return value;
        }

        /// <summary>
        /// Returns the current value as base64-decoded bytes.
        /// </summary>
        /// <returns>The decoded bytes.</returns>
        public byte[] GetBytesFromBase64()
        {
            var utf8 = TypedValueUtf8(out var rented);
            var parsed = XmlLexical.TryDecodeBase64(utf8, out var value);
            XmlDocument.ReturnScratch(rented);
            if (!parsed)
            {
                throw new XmlException("The value is not valid base64.");
            }

            return value!;
        }

        /// <summary>
        /// Returns the named child element.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <returns>The child cursor.</returns>
        public XmlElement GetChild(string localName)
        {
            if (!TryGetChild(localName, out var value))
            {
                throw new XmlException($"Child element '{localName}' was not found.");
            }

            return value;
        }

        /// <summary>
        /// Returns the named child element in <paramref name="namespaceUri"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <returns>The child cursor.</returns>
        public XmlElement GetChild(string localName, string namespaceUri)
        {
            if (!TryGetChild(localName, namespaceUri, out var value))
            {
                throw new XmlException($"Child element '{localName}' was not found.");
            }

            return value;
        }

        /// <summary>
        /// Returns the current value as a <see cref="DateTime"/>.
        /// </summary>
        /// <returns>The parsed value.</returns>
        public DateTime GetDateTime()
        {
            var utf8 = TypedValueUtf8(out var rented);
            var parsed = XmlLexical.TryParseDateTime(utf8, out var value);
            XmlDocument.ReturnScratch(rented);
            if (!parsed)
            {
                throw new XmlException("The value is not a valid dateTime.");
            }

            return value;
        }

        /// <summary>
        /// Returns the current value as a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <returns>The parsed value.</returns>
        public DateTimeOffset GetDateTimeOffset()
        {
            var utf8 = TypedValueUtf8(out var rented);
            var parsed = XmlLexical.TryParseDateTimeOffset(utf8, out var value);
            XmlDocument.ReturnScratch(rented);
            if (!parsed)
            {
                throw new XmlException("The value is not a valid dateTime.");
            }

            return value;
        }

        /// <summary>
        /// Returns the current value as a decimal.
        /// </summary>
        /// <returns>The parsed value.</returns>
        public decimal GetDecimal()
        {
            var utf8 = TypedValueUtf8(out var rented);
            var parsed = XmlLexical.TryParseDecimal(utf8, out var value);
            XmlDocument.ReturnScratch(rented);
            if (!parsed)
            {
                throw new XmlException("The value is not a valid decimal.");
            }

            return value;
        }

        /// <summary>
        /// Returns the current value as a double.
        /// </summary>
        /// <returns>The parsed value.</returns>
        public double GetDouble()
        {
            var utf8 = TypedValueUtf8(out var rented);
            var parsed = XmlLexical.TryParseDouble(utf8, out var value);
            XmlDocument.ReturnScratch(rented);
            if (!parsed)
            {
                throw new XmlException("The value is not a valid double.");
            }

            return value;
        }

        /// <summary>
        /// Returns the current value as a <see cref="Guid"/>.
        /// </summary>
        /// <returns>The parsed value.</returns>
        public Guid GetGuid()
        {
            var utf8 = TypedValueUtf8(out var rented);
            var parsed = XmlLexical.TryParseGuid(utf8, out var value);
            XmlDocument.ReturnScratch(rented);
            if (!parsed)
            {
                throw new XmlException("The value is not a valid GUID.");
            }

            return value;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(_document, _index);
        }

        /// <summary>
        /// Returns concatenated descendant text.
        /// </summary>
        /// <returns>The inner text.</returns>
        public string GetInnerText()
        {
            if (EnsureDocument().Row(_index).Kind is XmlValueKind.Comment or XmlValueKind.ProcessingInstruction)
            {
                return string.Empty;
            }

            return ValueString();
        }

        /// <summary>
        /// Returns the current value as a 32-bit integer.
        /// </summary>
        /// <returns>The parsed value.</returns>
        public int GetInt32()
        {
            var utf8 = TypedValueUtf8(out var rented);
            var parsed = XmlLexical.TryParseInt32(utf8, out var value);
            XmlDocument.ReturnScratch(rented);
            if (!parsed)
            {
                throw new XmlException("The value is not a valid integer.");
            }

            return value;
        }

        /// <summary>
        /// Returns the current value as a 64-bit integer.
        /// </summary>
        /// <returns>The parsed value.</returns>
        public long GetInt64()
        {
            var utf8 = TypedValueUtf8(out var rented);
            var parsed = XmlLexical.TryParseInt64(utf8, out var value);
            XmlDocument.ReturnScratch(rented);
            if (!parsed)
            {
                throw new XmlException("The value is not a valid integer.");
            }

            return value;
        }

        /// <summary>
        /// Reconstructs this node as XML text. An attribute has no markup outside its element, so the result is empty for one.
        /// </summary>
        /// <returns>The XML markup.</returns>
        public string GetRawXml()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            WriteTo(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        /// <summary>
        /// Returns the current value as a string.
        /// </summary>
        /// <returns>The string value, or <see langword="null"/> when this element is nil.</returns>
        public string? GetString()
        {
            return IsNil ? null : ValueString();
        }

        /// <summary>
        /// Attempts to get the named attribute.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">When this method returns true, the attribute cursor.</param>
        /// <returns><see langword="true"/> if the attribute exists.</returns>
        public bool TryGetAttribute(string localName, out XmlElement value)
        {
            ArgumentNullException.ThrowIfNull(localName);
            return TryFindAttribute(localName, null, out value);
        }

        /// <summary>
        /// Attempts to get the named child element.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">When this method returns true, the child cursor.</param>
        /// <returns><see langword="true"/> if the child exists.</returns>
        public bool TryGetChild(string localName, out XmlElement value)
        {
            ArgumentNullException.ThrowIfNull(localName);
            return TryFindChild(localName, null, out value);
        }

        /// <summary>
        /// Attempts to get the named child element in <paramref name="namespaceUri"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <param name="value">When this method returns true, the child cursor.</param>
        /// <returns><see langword="true"/> if the child exists.</returns>
        public bool TryGetChild(string localName, string namespaceUri, out XmlElement value)
        {
            ArgumentNullException.ThrowIfNull(localName);
            ArgumentNullException.ThrowIfNull(namespaceUri);
            return TryFindChild(localName, namespaceUri, out value);
        }

        /// <summary>
        /// Writes this node to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The destination writer.</param>
        public void WriteTo(Utf8XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            EnsureDocument().WriteRow(_index, writer);
        }

        /// <summary>
        /// Compares two cursors.
        /// </summary>
        /// <param name="left">The first cursor.</param>
        /// <param name="right">The second cursor.</param>
        /// <returns><see langword="true"/> if both point at the same row.</returns>
        public static bool operator ==(XmlElement left, XmlElement right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two cursors.
        /// </summary>
        /// <param name="left">The first cursor.</param>
        /// <param name="right">The second cursor.</param>
        /// <returns><see langword="true"/> if the cursors differ.</returns>
        public static bool operator !=(XmlElement left, XmlElement right)
        {
            return !left.Equals(right);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Returns the child row range of this cursor.
        /// </summary>
        /// <param name="document">The owning document.</param>
        /// <param name="first">When this method returns, the first child row.</param>
        /// <param name="end">When this method returns, the row after the last descendant.</param>
        /// <remarks>
        /// Children start after the attribute rows. The range is empty for anything that is not an element.
        /// </remarks>
        internal void ChildRange(XmlDocument document, out int first, out int end)
        {
            ref var row = ref document.Row(_index);
            if (!XmlDocument.IsElementKind(row.Kind))
            {
                first = 0;
                end = 0;
                return;
            }

            first = _index + 1 + row.AttributeCount;
            end = row.EndIndex;
        }

        /// <summary>
        /// Returns the owning document, throwing when this cursor is empty or the document was disposed.
        /// </summary>
        /// <returns>The live document.</returns>
        internal XmlDocument EnsureDocument()
        {
            if (_document is null)
            {
                throw new InvalidOperationException("The XML element is empty.");
            }

            _document.ThrowIfDisposed();
            return _document;
        }

        /// <summary>
        /// Finds an attribute by local name and, optionally, namespace.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI, or <see langword="null"/> to match any.</param>
        /// <param name="value">When this method returns true, the attribute cursor.</param>
        /// <returns><see langword="true"/> if the attribute exists.</returns>
        internal bool TryFindAttribute(string localName, string? namespaceUri, out XmlElement value)
        {
            var document = EnsureDocument();
            ref var row = ref document.Row(_index);
            if (XmlDocument.IsElementKind(row.Kind))
            {
                var end = _index + 1 + row.AttributeCount;
                for (var i = _index + 1; i < end; i++)
                {
                    ref var attribute = ref document.Row(i);
                    if (XmlDocument.Utf8Equals(document.LocalName(in attribute), localName)
                        && (namespaceUri is null || XmlDocument.Utf8Equals(document.NamespaceUri(in attribute), namespaceUri)))
                    {
                        value = new XmlElement(document, i);
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Finds a child element by local name and, optionally, namespace.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI, or <see langword="null"/> to match any.</param>
        /// <param name="value">When this method returns true, the child cursor.</param>
        /// <returns><see langword="true"/> if the child exists.</returns>
        internal bool TryFindChild(string localName, string? namespaceUri, out XmlElement value)
        {
            var document = EnsureDocument();
            ChildRange(document, out var first, out var end);
            for (var i = first; i < end; i = document.NextSiblingIndex(i))
            {
                ref var child = ref document.Row(i);
                if (XmlDocument.IsElementKind(child.Kind)
                    && XmlDocument.Utf8Equals(document.LocalName(in child), localName)
                    && (namespaceUri is null || XmlDocument.Utf8Equals(document.NamespaceUri(in child), namespaceUri)))
                {
                    value = new XmlElement(document, i);
                    return true;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Returns the UTF-8 value a typed getter should parse.
        /// </summary>
        /// <param name="rented">A pooled array to return with <see cref="XmlDocument.ReturnScratch"/>, or <see langword="null"/>.</param>
        /// <returns>The value bytes.</returns>
        /// <exception cref="XmlException">This element carries <c>xsi:nil="true"</c> and therefore has no value.</exception>
        internal ReadOnlySpan<byte> TypedValueUtf8(out byte[]? rented)
        {
            return ValueUtf8(out rented, typed: true);
        }

        /// <summary>
        /// Returns the value of this node as a string.
        /// </summary>
        /// <returns>The attribute or leaf value, or concatenated descendant text for elements.</returns>
        internal string ValueString()
        {
            var utf8 = ValueUtf8(out var rented);
            var value = Encoding.UTF8.GetString(utf8);
            XmlDocument.ReturnScratch(rented);
            return value;
        }

        /// <summary>
        /// Returns the UTF-8 value of this node, or concatenated descendant text for elements.
        /// </summary>
        /// <param name="rented">A pooled array to return with <see cref="XmlDocument.ReturnScratch"/>, or <see langword="null"/>.</param>
        /// <param name="typed">True when a typed getter is asking, in which case a nil element throws because it has no value.</param>
        /// <returns>The value bytes.</returns>
        /// <remarks>
        /// An element with a single text or CDATA child, which is the usual typed-value shape, returns that
        /// child's bytes directly. Because rows are in document order, mixed content is every text and CDATA
        /// row inside the element's row range, concatenated into a pooled buffer.
        /// </remarks>
        internal ReadOnlySpan<byte> ValueUtf8(out byte[]? rented, bool typed = false)
        {
            rented = null;
            var document = EnsureDocument();
            ref var row = ref document.Row(_index);
            if (!XmlDocument.IsElementKind(row.Kind))
            {
                return document.Value(in row);
            }

            if (typed && row.Kind is XmlValueKind.Nil)
            {
                throw new XmlException("The element is nil and has no value.");
            }

            var first = _index + 1 + row.AttributeCount;
            var end = row.EndIndex;
            if (end - first == 1)
            {
                ref var only = ref document.Row(first);
                return only.Kind is XmlValueKind.Text or XmlValueKind.CData ? document.Value(in only) : [];
            }

            var length = 0;
            for (var i = first; i < end; i++)
            {
                ref var descendant = ref document.Row(i);
                if (descendant.Kind is XmlValueKind.Text or XmlValueKind.CData)
                {
                    length += descendant.ValueLength;
                }
            }

            if (length == 0)
            {
                return [];
            }

            rented = ArrayPool<byte>.Shared.Rent(length);
            var written = 0;
            for (var i = first; i < end; i++)
            {
                ref var descendant = ref document.Row(i);
                if (descendant.Kind is XmlValueKind.Text or XmlValueKind.CData)
                {
                    document.Value(in descendant).CopyTo(rented.AsSpan(written));
                    written += descendant.ValueLength;
                }
            }

            return rented.AsSpan(0, written);
        }

        #endregion

        /// <summary>
        /// Enumerates child nodes of an <see cref="XmlElement"/>.
        /// </summary>
        public struct Enumerator
        {

            #region Fields

            private XmlElement _current;
            private readonly XmlDocument _document;
            private readonly int _end;
            private int _next;

            #endregion

            #region Public Properties

            /// <summary>
            /// Gets the current child.
            /// </summary>
            public readonly XmlElement Current => _current;

            #endregion

            #region Constructors

            /// <summary>
            /// Creates an enumerator over the child rows from <paramref name="first"/> up to <paramref name="end"/>.
            /// </summary>
            /// <param name="document">The owning document.</param>
            /// <param name="first">The first child row.</param>
            /// <param name="end">The row after the parent's last descendant.</param>
            internal Enumerator(XmlDocument document, int first, int end)
            {
                _document = document;
                _next = first;
                _end = end;
                _current = default;
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// Returns this enumerator.
            /// </summary>
            /// <returns>This enumerator.</returns>
            public readonly Enumerator GetEnumerator()
            {
                return this;
            }

            /// <summary>
            /// Advances to the next child.
            /// </summary>
            /// <returns><see langword="true"/> if a child is available.</returns>
            public bool MoveNext()
            {
                if (_next >= _end)
                {
                    return false;
                }

                _document.ThrowIfDisposed();
                _current = new XmlElement(_document, _next);
                _next = _document.NextSiblingIndex(_next);
                return true;
            }

            #endregion

        }

        /// <summary>
        /// Enumerates attributes of an <see cref="XmlElement"/>.
        /// </summary>
        public struct AttributeEnumerator
        {

            #region Fields

            private XmlElement _current;
            private readonly XmlDocument _document;
            private readonly int _end;
            private int _next;

            #endregion

            #region Public Properties

            /// <summary>
            /// Gets the current attribute.
            /// </summary>
            public readonly XmlElement Current => _current;

            #endregion

            #region Constructors

            /// <summary>
            /// Creates an enumerator over the attribute rows from <paramref name="first"/> up to <paramref name="end"/>.
            /// </summary>
            /// <param name="document">The owning document.</param>
            /// <param name="first">The first attribute row.</param>
            /// <param name="end">The row after the last attribute.</param>
            internal AttributeEnumerator(XmlDocument document, int first, int end)
            {
                _document = document;
                _next = first;
                _end = end;
                _current = default;
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// Returns this enumerator.
            /// </summary>
            /// <returns>This enumerator.</returns>
            public readonly AttributeEnumerator GetEnumerator()
            {
                return this;
            }

            /// <summary>
            /// Advances to the next attribute.
            /// </summary>
            /// <returns><see langword="true"/> if an attribute is available.</returns>
            public bool MoveNext()
            {
                if (_next >= _end)
                {
                    return false;
                }

                _document.ThrowIfDisposed();
                _current = new XmlElement(_document, _next);
                _next++;
                return true;
            }

            #endregion

        }

    }

}
