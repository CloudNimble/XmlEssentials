using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CloudNimble.Text.Xml
{

    public ref partial struct Utf8XmlReader
    {

        #region Private Methods

        /// <summary>
        /// Applies xmlns attributes on the current start tag, then resolves the element and attribute namespaces.
        /// </summary>
        /// <param name="bindingDepth">The depth that owns these declarations (current depth + 1).</param>
        internal void ApplyNamespaces(int bindingDepth)
        {
            if (_hasNamespaceAttribute)
            {
                for (var i = 0; i < _attributeCount; i++)
                {
                    var attribute = AttributeAt(i);
                    var prefix = AttributePrefix(attribute);
                    var local = AttributeLocalName(attribute);
                    if (prefix.IsEmpty && local.SequenceEqual("xmlns"u8))
                    {
                        PushBinding(bindingDepth, [], AttributeValue(attribute));
                        continue;
                    }

                    if (!prefix.SequenceEqual("xmlns"u8))
                    {
                        continue;
                    }

                    if (local.SequenceEqual("xmlns"u8))
                    {
                        throw Error("The xmlns prefix must not be declared.");
                    }

                    var uri = AttributeValue(attribute);
                    if (local.SequenceEqual("xml"u8))
                    {
                        if (!uri.SequenceEqual(XmlUtf8.Xml))
                        {
                            throw Error("The xml prefix must not be bound to another namespace.");
                        }

                        continue;
                    }

                    if (uri.IsEmpty && _version is XmlVersion.Version10)
                    {
                        throw Error("Prefix undeclaration is not allowed in XML 1.0.");
                    }

                    PushBinding(bindingDepth, local, uri);
                }
            }

            SetCurrentNamespace(ResolveElementNamespace(_prefixLength > 0
                ? _span.Slice(_nameStart, _prefixLength)
                : []));
            if (!_isEmptyElement && (_currentNsLength > 0 || _nsCount > 0))
            {
                StoreElementNamespace(_elementCount, CurrentNamespaceBytes());
            }
        }

        /// <summary>
        /// Returns the attribute record at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The attribute index.</param>
        /// <returns>The attribute record.</returns>
        internal readonly AttributeRecord AttributeAt(int index)
        {
            if (index < 8)
            {
                return _attributesInline[index];
            }

            return _attributes![index];
        }

        /// <summary>
        /// Returns the local name of <paramref name="attribute"/>.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns>UTF-8 local name bytes in the current buffer.</returns>
        internal readonly ReadOnlySpan<byte> AttributeLocalName(AttributeRecord attribute)
        {
            var adj = attribute.PrefixLength > 0 ? attribute.PrefixLength + 1 : 0;
            return _span.Slice(attribute.NameStart + adj, attribute.NameLength - adj);
        }

        /// <summary>
        /// Returns the prefix of <paramref name="attribute"/>.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns>UTF-8 prefix bytes, or empty when unprefixed.</returns>
        internal readonly ReadOnlySpan<byte> AttributePrefix(AttributeRecord attribute)
        {
            if (attribute.PrefixLength == 0)
            {
                return [];
            }

            return _span.Slice(attribute.NameStart, attribute.PrefixLength);
        }

        /// <summary>
        /// Returns the attribute value, decoded when entities or whitespace normalization applied.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns>UTF-8 value bytes.</returns>
        internal readonly ReadOnlySpan<byte> AttributeValue(AttributeRecord attribute)
        {
            if (attribute.Decoded is { } decoded)
            {
                return decoded.AsSpan(0, attribute.DecodedLength);
            }

            return _span.Slice(attribute.ValueStart, attribute.ValueLength);
        }

        /// <summary>
        /// Throws if two attributes on the current start tag share local name and namespace URI.
        /// </summary>
        internal readonly void CheckDuplicateNamespacedAttributes()
        {
            if (_attributeCount == 0)
            {
                return;
            }

            if (_attributeCount == 1)
            {
                _ = ResolveAttributeNamespace(AttributeAt(0));
                return;
            }

            for (var i = 0; i < _attributeCount; i++)
            {
                var left = AttributeAt(i);
                var leftLocal = AttributeLocalName(left);
                var leftNs = ResolveAttributeNamespace(left);
                for (var j = i + 1; j < _attributeCount; j++)
                {
                    var right = AttributeAt(j);
                    if (leftLocal.SequenceEqual(AttributeLocalName(right))
                        && leftNs.SequenceEqual(ResolveAttributeNamespace(right)))
                    {
                        throw Error("Duplicate attribute.");
                    }
                }
            }
        }

        /// <summary>
        /// Returns the namespace URI of the current element (start or end).
        /// </summary>
        /// <returns>UTF-8 URI bytes. Empty when the name is in no namespace.</returns>
        /// <remarks>
        /// End tags and empty elements keep the URI on the token after the matching
        /// scope has been popped so <see cref="Utf8XmlReader.NamespaceUriSpan"/> still
        /// names the element the caller just read.
        /// </remarks>
        internal readonly ReadOnlySpan<byte> CurrentElementNamespace()
        {
            if (_isEmptyElement || _tokenType is XmlTokenType.EndElement)
            {
                return CurrentNamespaceBytes();
            }

            if (_elementCount == 0)
            {
                return [];
            }

            return ElementNamespace(_elementCount - 1);
        }

        /// <summary>
        /// Returns the namespace URI of the in-progress element at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The element-stack index.</param>
        /// <returns>UTF-8 URI bytes.</returns>
        internal readonly ReadOnlySpan<byte> ElementNamespace(int index)
        {
            if (index < _resumeNameCount && _resumeNsUris is not null)
            {
                return _resumeNsUris[index];
            }

            int start;
            int length;
            if (index < 8)
            {
                start = _elementNsStartsInline[index];
                length = _elementNsLengthsInline[index];
                if (start >= 0)
                {
                    return _span.Slice(start, length);
                }

                return _elementNsUris is null ? [] : _elementNsUris[index];
            }

            start = _elementNsStarts![index];
            length = _elementNsLengths![index];
            if (start >= 0)
            {
                return _span.Slice(start, length);
            }

            return _elementNsUris![index];
        }

        /// <summary>
        /// Returns the prefix bytes of <paramref name="binding"/>.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <returns>UTF-8 prefix bytes.</returns>
        internal readonly ReadOnlySpan<byte> BindingPrefix(NamespaceBinding binding)
        {
            if (binding.PrefixStart >= 0)
            {
                return _span.Slice(binding.PrefixStart, binding.PrefixLength);
            }

            return binding.PrefixOwned;
        }

        /// <summary>
        /// Returns the URI bytes of <paramref name="binding"/>.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <returns>UTF-8 URI bytes.</returns>
        internal readonly ReadOnlySpan<byte> BindingUri(NamespaceBinding binding)
        {
            if (binding.UriStart >= 0)
            {
                return _span.Slice(binding.UriStart, binding.UriLength);
            }

            return binding.UriOwned;
        }

        /// <summary>
        /// Returns the namespace binding at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The binding index.</param>
        /// <returns>The binding.</returns>
        internal readonly NamespaceBinding BindingAt(int index)
        {
            if (index < 8)
            {
                return _nsBindingsInline[index];
            }

            return _nsBindings![index];
        }

        /// <summary>
        /// Returns the current element namespace URI bytes.
        /// </summary>
        /// <returns>UTF-8 URI bytes.</returns>
        internal readonly ReadOnlySpan<byte> CurrentNamespaceBytes()
        {
            if (_currentNsStart >= 0)
            {
                return _span.Slice(_currentNsStart, _currentNsLength);
            }

            return _currentNsOwned;
        }

        /// <summary>
        /// Grows the per-element namespace URI stack past the eight inline slots.
        /// </summary>
        internal void EnsureElementNsCapacity(int index)
        {
            if (_elementNsStarts is not null && index < _elementNsStarts.Length)
            {
                return;
            }

            var first = _elementNsStarts is null;
            var n = first ? 16 : _elementNsStarts!.Length * 2;
            while (n <= index)
            {
                n *= 2;
            }

            Array.Resize(ref _elementNsStarts, n);
            Array.Resize(ref _elementNsLengths, n);
            Array.Resize(ref _elementNsUris, n);
            if (first)
            {
                for (var i = 0; i < 8; i++)
                {
                    _elementNsStarts[i] = _elementNsStartsInline[i];
                    _elementNsLengths[i] = _elementNsLengthsInline[i];
                }
            }
        }

        /// <summary>
        /// Interns <paramref name="prefix"/> to a reusable byte array.
        /// </summary>
        /// <param name="prefix">The prefix bytes.</param>
        /// <returns>An interned or copied prefix.</returns>
        internal byte[] InternPrefix(scoped ReadOnlySpan<byte> prefix)
        {
            if (prefix.IsEmpty)
            {
                return XmlUtf8.Empty;
            }

            return InternUri(prefix);
        }

        /// <summary>
        /// Interns <paramref name="uri"/> to a reusable byte array.
        /// </summary>
        /// <param name="uri">The URI bytes.</param>
        /// <returns>An interned or copied URI.</returns>
        internal byte[] InternUri(scoped ReadOnlySpan<byte> uri)
        {
            var known = XmlUtf8.TryInternKnown(uri);
            if (known is not null)
            {
                return known;
            }

            if (_internedUris is not null)
            {
                for (var i = 0; i < _internedUriCount; i++)
                {
                    if (uri.SequenceEqual(_internedUris[i]))
                    {
                        return _internedUris[i];
                    }
                }
            }

            var copy = uri.ToArray();
            if (_internedUris is null)
            {
                _internedUris = new byte[4][];
            }
            else if (_internedUriCount == _internedUris.Length)
            {
                Array.Resize(ref _internedUris, _internedUris.Length * 2);
            }

            _internedUris[_internedUriCount] = copy;
            _internedUriCount++;
            return copy;
        }

        /// <summary>
        /// Looks up <paramref name="prefix"/> on the in-scope namespace stack.
        /// </summary>
        /// <param name="prefix">The prefix, or empty for the default xmlns.</param>
        /// <param name="declared">True when the prefix is in scope, including the built-in xml and xmlns prefixes.</param>
        /// <returns>The URI, or empty if unbound.</returns>
        internal readonly ReadOnlySpan<byte> LookupNamespace(scoped ReadOnlySpan<byte> prefix, out bool declared)
        {
            if (prefix.SequenceEqual("xml"u8))
            {
                declared = true;
                return XmlUtf8.Xml;
            }

            if (prefix.SequenceEqual("xmlns"u8))
            {
                declared = true;
                return XmlUtf8.Xmlns;
            }

            for (var i = _nsCount - 1; i >= 0; i--)
            {
                var binding = BindingAt(i);
                if (prefix.SequenceEqual(BindingPrefix(binding)))
                {
                    declared = true;
                    return BindingUri(binding);
                }
            }

            declared = prefix.IsEmpty;
            return [];
        }

        /// <summary>
        /// Copies in-progress element namespace URIs for a snapshot.
        /// </summary>
        /// <returns>One URI string per open element.</returns>
        internal readonly string[] MaterializeElementNamespaces()
        {
            var names = new string[_elementCount];
            for (var i = 0; i < _elementCount; i++)
            {
                names[i] = Encoding.UTF8.GetString(ElementNamespace(i));
            }

            return names;
        }

        /// <summary>
        /// Copies binding depths for a snapshot.
        /// </summary>
        /// <returns>One depth per in-scope binding.</returns>
        internal readonly int[] MaterializeNamespaceDepths()
        {
            var depths = new int[_nsCount];
            for (var i = 0; i < _nsCount; i++)
            {
                depths[i] = BindingAt(i).Depth;
            }

            return depths;
        }

        /// <summary>
        /// Copies binding prefixes for a snapshot.
        /// </summary>
        /// <returns>One prefix per in-scope binding.</returns>
        internal readonly string[] MaterializeNamespacePrefixes()
        {
            var prefixes = new string[_nsCount];
            for (var i = 0; i < _nsCount; i++)
            {
                prefixes[i] = Encoding.UTF8.GetString(BindingPrefix(BindingAt(i)));
            }

            return prefixes;
        }

        /// <summary>
        /// Copies binding URIs for a snapshot.
        /// </summary>
        /// <returns>One URI per in-scope binding.</returns>
        internal readonly string[] MaterializeNamespaceUris()
        {
            var uris = new string[_nsCount];
            for (var i = 0; i < _nsCount; i++)
            {
                uris[i] = Encoding.UTF8.GetString(BindingUri(BindingAt(i)));
            }

            return uris;
        }

        /// <summary>
        /// Removes namespace bindings whose depth is <paramref name="depth"/>.
        /// </summary>
        /// <param name="depth">The element depth that is ending.</param>
        internal void PopNamespaceScope(int depth)
        {
            while (_nsCount > 0 && BindingAt(_nsCount - 1).Depth == depth)
            {
                _nsCount--;
            }
        }

        /// <summary>
        /// Pushes a namespace binding.
        /// </summary>
        /// <param name="depth">The owning element depth.</param>
        /// <param name="prefix">The prefix bytes. Empty for the default xmlns.</param>
        /// <param name="uri">The URI bytes. Empty when unbound.</param>
        internal void PushBinding(int depth, byte[] prefix, byte[] uri)
        {
            StoreBinding(new NamespaceBinding(depth, prefix, uri));
        }

        /// <summary>
        /// Pushes a namespace binding from spans of the current buffer or owned copies.
        /// </summary>
        /// <param name="depth">The owning element depth.</param>
        /// <param name="prefix">The prefix bytes. Empty for the default xmlns.</param>
        /// <param name="uri">The URI bytes. Empty when unbound.</param>
        internal void PushBinding(int depth, scoped ReadOnlySpan<byte> prefix, scoped ReadOnlySpan<byte> uri)
        {
            StoreBinding(CreateBinding(depth, prefix, uri));
        }

        /// <summary>
        /// Appends <paramref name="binding"/> to the inline or heap namespace stack.
        /// </summary>
        /// <param name="binding">The binding to store.</param>
        internal void StoreBinding(NamespaceBinding binding)
        {
            if (_nsCount < 8)
            {
                _nsBindingsInline[_nsCount] = binding;
            }
            else
            {
                if (_nsBindings is null)
                {
                    _nsBindings = new NamespaceBinding[16];
                    for (var i = 0; i < 8; i++)
                    {
                        _nsBindings[i] = _nsBindingsInline[i];
                    }
                }
                else if (_nsCount == _nsBindings.Length)
                {
                    Array.Resize(ref _nsBindings, _nsBindings.Length * 2);
                }

                _nsBindings[_nsCount] = binding;
            }

            _nsCount++;
        }

        /// <summary>
        /// Resolves the namespace of an attribute from its prefix.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns>UTF-8 URI bytes.</returns>
        internal readonly ReadOnlySpan<byte> ResolveAttributeNamespace(AttributeRecord attribute)
        {
            var prefix = AttributePrefix(attribute);
            var local = AttributeLocalName(attribute);
            if (prefix.IsEmpty && local.SequenceEqual("xmlns"u8))
            {
                return XmlUtf8.Xmlns;
            }

            if (prefix.SequenceEqual("xmlns"u8))
            {
                return XmlUtf8.Xmlns;
            }

            if (prefix.IsEmpty)
            {
                return [];
            }

            var uri = LookupNamespace(prefix, out var declared);
            if (!declared || uri.IsEmpty)
            {
                throw Error("Undeclared namespace prefix.");
            }

            return uri;
        }

        /// <summary>
        /// Resolves the namespace of the current element from its prefix or the default xmlns.
        /// </summary>
        /// <param name="prefix">The element prefix, or empty.</param>
        /// <returns>The URI bytes in the current buffer or an interned copy.</returns>
        internal ReadOnlySpan<byte> ResolveElementNamespace(scoped ReadOnlySpan<byte> prefix)
        {
            var uri = LookupNamespace(prefix, out var declared);
            if (!prefix.IsEmpty && (!declared || uri.IsEmpty))
            {
                throw Error("Undeclared namespace prefix.");
            }

            return uri;
        }

        /// <summary>
        /// Stores <paramref name="uri"/> as the current element namespace.
        /// </summary>
        /// <param name="uri">The URI bytes.</param>
        internal void SetCurrentNamespace(scoped ReadOnlySpan<byte> uri)
        {
            if (uri.IsEmpty)
            {
                _currentNsStart = -1;
                _currentNsLength = 0;
                _currentNsOwned = XmlUtf8.Empty;
                return;
            }

            var known = XmlUtf8.TryInternKnown(uri);
            if (known is not null)
            {
                _currentNsStart = -1;
                _currentNsLength = known.Length;
                _currentNsOwned = known;
                return;
            }

            if (TryGetBufferOffset(uri, out var start))
            {
                _currentNsStart = start;
                _currentNsLength = uri.Length;
                _currentNsOwned = XmlUtf8.Empty;
                return;
            }

            _currentNsOwned = InternUri(uri);
            _currentNsStart = -1;
            _currentNsLength = uri.Length;
        }

        /// <summary>
        /// Stores <paramref name="uri"/> on the element stack at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The element-stack index.</param>
        /// <param name="uri">The URI bytes.</param>
        internal void StoreElementNamespace(int index, scoped ReadOnlySpan<byte> uri)
        {
            var start = -1;
            byte[]? owned = null;
            if (!uri.IsEmpty)
            {
                var known = XmlUtf8.TryInternKnown(uri);
                if (known is not null)
                {
                    owned = known;
                }
                else if (TryGetBufferOffset(uri, out var offset))
                {
                    start = offset;
                }
                else
                {
                    owned = InternUri(uri);
                }
            }

            if (index < 8)
            {
                _elementNsStartsInline[index] = start;
                _elementNsLengthsInline[index] = uri.Length;
                if (owned is not null)
                {
                    _elementNsUris ??= new byte[8][];
                    _elementNsUris[index] = owned;
                }

                return;
            }

            EnsureElementNsCapacity(index);
            _elementNsStarts![index] = start;
            _elementNsLengths![index] = uri.Length;
            if (owned is not null)
            {
                _elementNsUris![index] = owned;
            }
        }

        /// <summary>
        /// Returns the offset of <paramref name="slice"/> in the current buffer.
        /// </summary>
        /// <param name="slice">A span that may alias <c>_span</c>.</param>
        /// <param name="start">When this method returns true, the byte offset.</param>
        /// <returns><see langword="true"/> if <paramref name="slice"/> is inside the current buffer.</returns>
        internal readonly bool TryGetBufferOffset(scoped ReadOnlySpan<byte> slice, out int start)
        {
            start = 0;
            if (slice.IsEmpty || slice.Length > _span.Length)
            {
                return false;
            }

            var offset = Unsafe.ByteOffset(
                ref MemoryMarshal.GetReference(_span),
                ref MemoryMarshal.GetReference(slice));
            if (offset < 0 || offset + slice.Length > _span.Length)
            {
                return false;
            }

            start = (int)offset;
            return true;
        }

        /// <summary>
        /// Creates a binding that slices the current buffer when possible.
        /// </summary>
        /// <param name="depth">The owning element depth.</param>
        /// <param name="prefix">The prefix bytes.</param>
        /// <param name="uri">The URI bytes.</param>
        /// <returns>The binding.</returns>
        internal NamespaceBinding CreateBinding(int depth, scoped ReadOnlySpan<byte> prefix, scoped ReadOnlySpan<byte> uri)
        {
            var prefixStart = -1;
            var prefixOwned = XmlUtf8.Empty;
            if (!prefix.IsEmpty)
            {
                if (TryGetBufferOffset(prefix, out var p))
                {
                    prefixStart = p;
                }
                else
                {
                    prefixOwned = InternPrefix(prefix);
                }
            }

            var uriStart = -1;
            var uriOwned = XmlUtf8.Empty;
            if (!uri.IsEmpty)
            {
                var known = XmlUtf8.TryInternKnown(uri);
                if (known is not null)
                {
                    uriOwned = known;
                }
                else if (TryGetBufferOffset(uri, out var u))
                {
                    uriStart = u;
                }
                else
                {
                    uriOwned = InternUri(uri);
                }
            }

            return new NamespaceBinding(depth, prefixStart, prefix.Length, prefixOwned, uriStart, uri.Length, uriOwned);
        }

        #endregion

    }

}
