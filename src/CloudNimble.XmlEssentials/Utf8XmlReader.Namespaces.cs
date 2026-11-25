using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
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
                    ref readonly var attribute = ref AttributeAt(i);
                    var prefix = AttributePrefix(attribute);
                    var local = AttributeLocalName(attribute);
                    if (prefix.IsEmpty && local.SequenceEqual("xmlns"u8))
                    {
                        var defaultUri = AttributeValue(attribute);
                        if (IsReservedNamespace(defaultUri))
                        {
                            throw Error("The xml and xmlns namespaces must not be declared as the default namespace.");
                        }

                        PushBinding(bindingDepth, [], defaultUri);
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

                    if (IsReservedNamespace(uri))
                    {
                        throw Error("The xml and xmlns namespaces must not be bound to another prefix.");
                    }

                    PushBinding(bindingDepth, local, uri);
                }
            }

            if (_prefixLength == 0)
            {
                ResolveUnprefixedElementNamespace();
            }
            else
            {
                var elementPrefix = _span.Slice(_nameStart, _prefixLength);
                if (_prefixLength == 5 && elementPrefix.SequenceEqual(XmlUtf8.XmlnsPrefix))
                {
                    throw Error("Element names must not use the xmlns prefix.");
                }

                if (_prefixLength == 3 && elementPrefix.SequenceEqual(XmlUtf8.XmlPrefix))
                {
                    SetCurrentNamespace(XmlUtf8.Xml);
                }
                else
                {
                    var index = LookupPrefixIndex(_nameStart, _prefixLength);
                    if (index < 0 || BindingUriLengthAt(index) == 0)
                    {
                        throw Error("Undeclared namespace prefix.");
                    }

                    AssignCurrentNamespace(BindingAt(index));
                }
            }

            if (_hasUnresolvedAttribute)
            {
                ResolveAttributeNamespaces();
            }

            if (!_isEmptyElement && (_currentNsLength > 0 || _nsCount > 0 || _elementNsStored))
            {
                StoreElementNamespaceFromCurrent(_elementCount);
            }
        }

        /// <summary>
        /// Copies URI slots from <paramref name="binding"/> onto the current element.
        /// </summary>
        /// <param name="binding">The in-scope binding.</param>
        internal void AssignCurrentNamespace(in NamespaceBinding binding)
        {
            _currentNsStart = binding.UriStart;
            _currentNsLength = binding.UriLength;
            _currentNsOwned = binding.UriStart >= 0 ? XmlUtf8.Empty : Owned(binding.UriStart);
        }

        /// <summary>
        /// Returns the attribute record at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The attribute index.</param>
        /// <returns>The attribute record.</returns>
        [UnscopedRef]
        internal readonly ref readonly AttributeRecord AttributeAt(int index)
        {
            if (index < 8)
            {
                return ref _attributesInline[index];
            }

            return ref _attributes[index];
        }

        /// <summary>
        /// Returns the local name of <paramref name="attribute"/>.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns>UTF-8 local name bytes in the current buffer.</returns>
        internal readonly ReadOnlySpan<byte> AttributeLocalName(scoped in AttributeRecord attribute)
        {
            var adj = attribute.PrefixLength > 0 ? attribute.PrefixLength + 1 : 0;
            return _span.Slice(attribute.NameStart + adj, attribute.NameLength - adj);
        }

        /// <summary>
        /// Returns the prefix of <paramref name="attribute"/>.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns>UTF-8 prefix bytes, or empty when unprefixed.</returns>
        internal readonly ReadOnlySpan<byte> AttributePrefix(scoped in AttributeRecord attribute)
        {
            if (attribute.PrefixLength == 0)
            {
                return [];
            }

            return _span.Slice(attribute.NameStart, attribute.PrefixLength);
        }

        /// <summary>
        /// Returns the qualified name of <paramref name="attribute"/>, including a prefix and colon when present.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns>UTF-8 name bytes in the current buffer.</returns>
        internal readonly ReadOnlySpan<byte> AttributeQualifiedName(scoped in AttributeRecord attribute)
        {
            return _span.Slice(attribute.NameStart, attribute.NameLength);
        }

        /// <summary>
        /// Returns the attribute value, decoded when entities or whitespace normalization applied.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns>UTF-8 value bytes.</returns>
        internal readonly ReadOnlySpan<byte> AttributeValue(scoped in AttributeRecord attribute)
        {
            if (attribute.DecodedStart >= 0)
            {
                return _attributeDecoded.Slice(attribute.DecodedStart, attribute.DecodedLength);
            }

            return _span.Slice(attribute.ValueStart, attribute.ValueLength);
        }

        /// <summary>
        /// Returns the namespace binding at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The binding index.</param>
        /// <returns>The binding.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly NamespaceBinding BindingAt(int index)
        {
            if (index < InlineSlots)
            {
                return _nsBindingsInline[index];
            }

            return _nsBindings[index];
        }

        /// <summary>
        /// Returns the owning depth of the namespace binding at <paramref name="index"/> without copying the binding.
        /// </summary>
        /// <param name="index">The binding index.</param>
        /// <returns>The element depth that declared the binding.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int BindingDepthAt(int index)
        {
            return index < InlineSlots ? _nsBindingsInline[index].Depth : _nsBindings[index].Depth;
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

            return Owned(binding.PrefixStart);
        }

        /// <summary>
        /// Returns the prefix byte length of the namespace binding at <paramref name="index"/> without copying the binding.
        /// </summary>
        /// <param name="index">The binding index.</param>
        /// <returns>The prefix length, or 0 for a default-namespace binding.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int BindingPrefixLengthAt(int index)
        {
            return index < InlineSlots ? _nsBindingsInline[index].PrefixLength : _nsBindings[index].PrefixLength;
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

            return Owned(binding.UriStart);
        }

        /// <summary>
        /// Returns the URI byte length of the namespace binding at <paramref name="index"/> without copying the binding.
        /// </summary>
        /// <param name="index">The binding index.</param>
        /// <returns>The URI length, or 0 when the binding undeclares its prefix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int BindingUriLengthAt(int index)
        {
            return index < InlineSlots ? _nsBindingsInline[index].UriLength : _nsBindings[index].UriLength;
        }

        /// <summary>
        /// Throws if two attributes on the current start tag share local name and namespace URI, for a start tag
        /// with many attributes.
        /// </summary>
        /// <remarks>
        /// Each attribute is put in a hash table by its local name and compared only with the attributes that
        /// landed in the same place, so the work grows with the count and not with its square. The table is on
        /// the stack for up to 128 attributes and is rented beyond that. The namespace is not part of the hash:
        /// two prefixes may be bound to one URI.
        /// </remarks>
        [SkipLocalsInit]
        internal readonly void CheckDuplicateAttributesByName()
        {
            // The table holds attribute positions plus one, so zero is an empty place. It is at least twice the
            // count, which keeps the runs of filled places short.
            var size = (int)BitOperations.RoundUpToPowerOf2((uint)_attributeCount * 2);
            int[]? rented = null;
            var table = size <= 256 ? stackalloc int[256] : (rented = ArrayPool<int>.Shared.Rent(size));
            table = table[..size];
            table.Clear();
            try
            {
                for (var i = 0; i < _attributeCount; i++)
                {
                    ref readonly var attribute = ref AttributeAt(i);
                    var local = AttributeLocalName(attribute);
                    var hash = 2166136261u;
                    foreach (var value in local)
                    {
                        hash = (hash ^ value) * 16777619u;
                    }

                    var place = (int)(hash & (uint)(size - 1));
                    while (table[place] != 0)
                    {
                        ref readonly var other = ref AttributeAt(table[place] - 1);
                        if (local.SequenceEqual(AttributeLocalName(other))
                            && (attribute.NsIndex == other.NsIndex
                                || ResolveAttributeNamespace(attribute).SequenceEqual(ResolveAttributeNamespace(other))))
                        {
                            throw Error("Duplicate attribute.");
                        }

                        place = (place + 1) & (size - 1);
                    }

                    table[place] = i + 1;
                }
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<int>.Shared.Return(rented);
                }
            }
        }

        /// <summary>
        /// Throws if two attributes on the current start tag share local name and namespace URI.
        /// </summary>
        /// <remarks>
        /// A start tag with a few attributes compares every pair. That grows with the square of the count, so a
        /// wider start tag is checked through a table of the names it has seen.
        /// </remarks>
        internal readonly void CheckDuplicateNamespacedAttributes()
        {
            if (_attributeCount > InlineSlots * 2)
            {
                CheckDuplicateAttributesByName();
                return;
            }

            for (var i = 0; i < _attributeCount; i++)
            {
                ref readonly var left = ref AttributeAt(i);
                var leftLocal = AttributeLocalName(left);
                for (var j = i + 1; j < _attributeCount; j++)
                {
                    // Two names of different lengths are different names, and for names with no prefix
                    // that is known from the records alone.
                    ref readonly var right = ref AttributeAt(j);
                    if (right.NameLength != left.NameLength && (left.PrefixLength | right.PrefixLength) == 0)
                    {
                        continue;
                    }

                    if (leftLocal.SequenceEqual(AttributeLocalName(right))
                        && (left.NsIndex == right.NsIndex
                            || ResolveAttributeNamespace(left).SequenceEqual(ResolveAttributeNamespace(right))))
                    {
                        throw Error("Duplicate attribute.");
                    }
                }
            }
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
            var prefixStart = OwnedEmpty;
            if (!prefix.IsEmpty)
            {
                // The prefix of a declaration is the local name of an xmlns attribute, always a slice of the buffer.
                TryGetBufferOffset(prefix, out prefixStart);
            }

            var uriStart = OwnedEmpty;
            if (!uri.IsEmpty)
            {
                var known = XmlUtf8.TryInternKnown(uri);
                if (known is not null)
                {
                    uriStart = OwnedStart(known);
                }
                else if (!TryGetBufferOffset(uri, out uriStart))
                {
                    uriStart = OwnedStart(InternUri(uri));
                }
            }

            return new NamespaceBinding(depth, prefixStart, prefix.Length, uriStart, uri.Length);
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

            return ElementNamespace(_elementCount - 1);
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
            if (index < InlineSlots)
            {
                start = _elementNsStartsInline[index];
                length = _elementNsLengthsInline[index];
            }
            else if (_elementNsStarts.IsEmpty || index >= _elementNsStarts.Length)
            {
                return [];
            }
            else
            {
                start = _elementNsStarts[index];
                length = _elementNsLengths[index];
            }

            if (start >= 0)
            {
                return _span.Slice(start, length);
            }

            // An owned URI is stored only for a non-empty namespace. A zero length means no namespace,
            // and the owned slot may still hold the URI of an earlier element at this depth.
            return length == 0 ? [] : _elementNsUris![index];
        }

        /// <summary>
        /// Grows the per-element namespace URI stack past the eight inline slots.
        /// </summary>
        internal void EnsureElementNsCapacity(int index)
        {
            if (!_elementNsStarts.IsEmpty && index < _elementNsStarts.Length)
            {
                return;
            }

            var first = _elementNsStarts.IsEmpty;
            var n = first ? InlineSlots * 2 : _elementNsStarts.Length * 2;
            while (n <= index)
            {
                n *= 2;
            }

            _elementNsStarts = Grow<int>(_elementNsStarts, _elementNsStarts.Length, n);
            _elementNsLengths = Grow<int>(_elementNsLengths, _elementNsLengths.Length, n);
            if (first)
            {
                for (var i = 0; i < InlineSlots; i++)
                {
                    _elementNsStarts[i] = _elementNsStartsInline[i];
                    _elementNsLengths[i] = _elementNsLengthsInline[i];
                }
            }
        }

        /// <summary>
        /// Returns the index of the innermost default-namespace binding, or -1 when none is in scope.
        /// </summary>
        /// <returns>The binding index, or -1.</returns>
        internal readonly int FindDefaultBinding()
        {
            for (var i = _nsCount - 1; i >= 0; i--)
            {
                if (BindingPrefixLengthAt(i) == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Interns a namespace URI or a prefix that is held in an array of its own, such as one from a reader
        /// state, without copying it.
        /// </summary>
        /// <param name="owned">The array, which is never written to again.</param>
        /// <returns>The interned array that holds the same bytes: <paramref name="owned"/> itself when it is new to this reader.</returns>
        internal byte[] InternOwned(byte[] owned)
        {
            var known = XmlUtf8.TryInternKnown(owned);
            if (known is not null)
            {
                return known;
            }

            for (var i = 0; i < _internedUriCount; i++)
            {
                if (owned.AsSpan().SequenceEqual(_internedUris![i]))
                {
                    return _internedUris[i];
                }
            }

            if (_internedUris is null)
            {
                _internedUris = new byte[4][];
            }
            else if (_internedUriCount == _internedUris.Length)
            {
                Array.Resize(ref _internedUris, _internedUris.Length * 2);
            }

            _internedUris[_internedUriCount] = owned;
            _internedUriCount++;
            return owned;
        }

        /// <summary>
        /// Interns a namespace URI or a prefix that is in the input, by copying it.
        /// </summary>
        /// <param name="uri">The bytes.</param>
        /// <returns>The interned array, which holds the same bytes.</returns>
        internal byte[] InternUri(scoped ReadOnlySpan<byte> uri)
        {
            for (var i = 0; i < _internedUriCount; i++)
            {
                if (uri.SequenceEqual(_internedUris![i]))
                {
                    return _internedUris[i];
                }
            }

            // The bytes are new to this reader, so they are copied out of the input and kept.
            return InternOwned(uri.ToArray());
        }

        /// <summary>
        /// Returns whether <paramref name="uri"/> is one of the two namespace names reserved by Namespaces in XML, section 3.
        /// </summary>
        /// <param name="uri">The namespace name from an xmlns declaration.</param>
        /// <returns><see langword="true"/> for the <c>xml</c> and <c>xmlns</c> namespace names.</returns>
        /// <remarks>
        /// Only the <c>xml</c> prefix may be bound to the first, and nothing may be bound to the second.
        /// The length test keeps ordinary declarations to a single comparison.
        /// </remarks>
        internal static bool IsReservedNamespace(ReadOnlySpan<byte> uri)
        {
            return (uri.Length == XmlUtf8.Xml.Length && uri.SequenceEqual(XmlUtf8.Xml))
                || (uri.Length == XmlUtf8.Xmlns.Length && uri.SequenceEqual(XmlUtf8.Xmlns));
        }

        /// <summary>
        /// Finds the in-scope binding for the prefix at <paramref name="start"/> in the current buffer.
        /// </summary>
        /// <param name="start">The byte offset of the prefix.</param>
        /// <param name="length">The prefix byte length, at least 1.</param>
        /// <returns>The binding index, or -1 when the prefix is not declared.</returns>
        /// <remarks>
        /// A document reuses a handful of prefixes thousands of times, and the bindings that declare them
        /// usually sit at the bottom of the stack (on the root element). This is the byte-level equivalent
        /// of the BCL's interned prefix: a small direct-mapped cache from prefix to binding index, emptied
        /// whenever a binding is pushed or popped, so a hit costs one short compare instead of a scan of
        /// every binding in scope.
        /// </remarks>
        internal int LookupPrefixIndex(int start, int length)
        {
            var slot = (_span[start] ^ (_span[start + length - 1] << 1) ^ length) & 7;
            if ((_nsPrefixCacheMask & (1 << slot)) != 0 && _nsPrefixCacheLengths[slot] == length)
            {
                var cached = _nsPrefixCacheStarts[slot];
                if (cached == start || _span.Slice(cached, length).SequenceEqual(_span.Slice(start, length)))
                {
                    return _nsPrefixCacheIndexes[slot];
                }
            }

            var prefix = _span.Slice(start, length);
            for (var i = _nsCount - 1; i >= 0; i--)
            {
                if (BindingPrefixLengthAt(i) != length || !BindingPrefix(BindingAt(i)).SequenceEqual(prefix))
                {
                    continue;
                }

                _nsPrefixCacheMask |= 1 << slot;
                _nsPrefixCacheStarts[slot] = start;
                _nsPrefixCacheLengths[slot] = length;
                _nsPrefixCacheIndexes[slot] = i;
                return i;
            }

            return -1;
        }

        /// <summary>
        /// Copies in-progress element namespace URIs for a snapshot.
        /// </summary>
        /// <returns>One URI string per open element.</returns>
        internal readonly byte[][] MaterializeElementNamespaces()
        {
            if (ResumedElementsAreOpen() && _resumeNsUris is { } resumed)
            {
                return resumed;
            }

            var names = _elementCount == 0 ? [] : new byte[_elementCount][];
            for (var i = 0; i < names.Length; i++)
            {
                names[i] = i < _resumeNameCount && _resumeNsUris is not null ? _resumeNsUris[i] : XmlUtf8.Owned(ElementNamespace(i));
            }

            return names;
        }

        /// <summary>
        /// Copies binding depths for a snapshot.
        /// </summary>
        /// <returns>One depth per in-scope binding.</returns>
        internal readonly int[] MaterializeNamespaceDepths()
        {
            var depths = _nsCount == 0 ? [] : new int[_nsCount];
            for (var i = 0; i < depths.Length; i++)
            {
                depths[i] = BindingAt(i).Depth;
            }

            return depths;
        }

        /// <summary>
        /// Copies binding prefixes for a snapshot.
        /// </summary>
        /// <returns>One prefix per in-scope binding.</returns>
        internal readonly byte[][] MaterializeNamespacePrefixes()
        {
            var prefixes = _nsCount == 0 ? [] : new byte[_nsCount][];
            for (var i = 0; i < prefixes.Length; i++)
            {
                var binding = BindingAt(i);
                prefixes[i] = binding.PrefixStart < 0 ? Owned(binding.PrefixStart) : XmlUtf8.Owned(BindingPrefix(binding));
            }

            return prefixes;
        }

        /// <summary>
        /// Copies binding URIs for a snapshot.
        /// </summary>
        /// <returns>One URI per in-scope binding.</returns>
        internal readonly byte[][] MaterializeNamespaceUris()
        {
            var uris = _nsCount == 0 ? [] : new byte[_nsCount][];
            for (var i = 0; i < uris.Length; i++)
            {
                var binding = BindingAt(i);
                uris[i] = binding.UriStart < 0 ? Owned(binding.UriStart) : XmlUtf8.Owned(BindingUri(binding));
            }

            return uris;
        }

        /// <summary>
        /// Returns the bytes a negative binding start names.
        /// </summary>
        /// <param name="start">A negative <see cref="NamespaceBinding.PrefixStart"/> or <see cref="NamespaceBinding.UriStart"/>.</param>
        /// <returns>The owned bytes.</returns>
        internal readonly byte[] Owned(int start)
        {
            return start switch
            {
                OwnedEmpty => XmlUtf8.Empty,
                OwnedXsi => XmlUtf8.Xsi,
                _ => _internedUris![OwnedFirstInterned - start],
            };
        }

        /// <summary>
        /// Returns the negative binding start that names <paramref name="owned"/>.
        /// </summary>
        /// <param name="owned">Empty, the <c>xsi</c> namespace, or an array returned by <see cref="InternUri"/>.</param>
        /// <returns>The start to store in a <see cref="NamespaceBinding"/>.</returns>
        internal readonly int OwnedStart(byte[] owned)
        {
            if (owned.Length == 0)
            {
                return OwnedEmpty;
            }

            return ReferenceEquals(owned, XmlUtf8.Xsi)
                ? OwnedXsi
                : OwnedFirstInterned - Array.IndexOf(_internedUris!, owned);
        }

        /// <summary>
        /// Removes namespace bindings whose depth is <paramref name="depth"/>.
        /// </summary>
        /// <param name="depth">The element depth that is ending.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PopNamespaceScope(int depth)
        {
            if (_nsCount == 0 || BindingDepthAt(_nsCount - 1) != depth)
            {
                return;
            }

            _nsPrefixCacheMask = 0;
            _nsDefaultIndex = NsIndexUnknown;
            do
            {
                _nsCount--;
            }
            while (_nsCount > 0 && BindingDepthAt(_nsCount - 1) == depth);
        }

        /// <summary>
        /// Pushes a namespace binding.
        /// </summary>
        /// <param name="depth">The owning element depth.</param>
        /// <param name="prefix">The prefix bytes. Empty for the default xmlns.</param>
        /// <param name="uri">The URI bytes. Empty when unbound.</param>
        internal void PushBinding(int depth, byte[] prefix, byte[] uri)
        {
            StoreBinding(new NamespaceBinding(depth, OwnedStart(prefix), prefix.Length, OwnedStart(uri), uri.Length));
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
        /// Resolves the namespace of an attribute from its prefix.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns>UTF-8 URI bytes.</returns>
        internal readonly ReadOnlySpan<byte> ResolveAttributeNamespace(scoped in AttributeRecord attribute)
        {
            return attribute.NsIndex switch
            {
                AttributeRecord.NoNamespace => [],
                AttributeRecord.XmlnsNamespace => XmlUtf8.Xmlns,
                AttributeRecord.XmlNamespace => XmlUtf8.Xml,
                _ => BindingUri(BindingAt(attribute.NsIndex - AttributeRecord.FirstBinding)),
            };
        }

        /// <summary>
        /// Looks up the prefix of every prefixed attribute on the current start tag and records its binding.
        /// </summary>
        /// <exception cref="XmlException">A prefix is not declared, or is undeclared in XML 1.1.</exception>
        /// <remarks>
        /// Runs once per start tag, after the tag's own xmlns declarations are in scope. Afterwards the
        /// duplicate check and <see cref="Utf8XmlReader.NamespaceUriSpan"/> read the recorded binding
        /// instead of searching the binding stack again.
        /// </remarks>
        internal void ResolveAttributeNamespaces()
        {
            for (var i = 0; i < _attributeCount; i++)
            {
                ref readonly var attribute = ref AttributeAt(i);
                if (attribute.NsIndex != AttributeRecord.UnresolvedNamespace)
                {
                    continue;
                }

                var index = LookupPrefixIndex(attribute.NameStart, attribute.PrefixLength);
                if (index < 0 || BindingUriLengthAt(index) == 0)
                {
                    throw Error("Undeclared namespace prefix.");
                }

                if (i < 8)
                {
                    _attributesInline[i].NsIndex = AttributeRecord.FirstBinding + index;
                }
                else
                {
                    _attributes[i].NsIndex = AttributeRecord.FirstBinding + index;
                }
            }
        }

        /// <summary>
        /// Makes the namespace of an element whose name has no prefix the current namespace: the default
        /// namespace in scope, or none.
        /// </summary>
        internal void ResolveUnprefixedElementNamespace()
        {
            if (_nsDefaultIndex == NsIndexUnknown)
            {
                _nsDefaultIndex = FindDefaultBinding();
            }

            if (_nsDefaultIndex < 0)
            {
                SetCurrentNamespace([]);
            }
            else
            {
                AssignCurrentNamespace(BindingAt(_nsDefaultIndex));
            }
        }

        /// <summary>
        /// Restores the current namespace URI from the element stack at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The element-stack index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RestoreCurrentNamespaceFromElement(int index)
        {
            int start;
            int length;
            if (index < InlineSlots)
            {
                start = _elementNsStartsInline[index];
                length = _elementNsLengthsInline[index];
            }
            else if (_elementNsStarts.IsEmpty || index >= _elementNsStarts.Length)
            {
                SetCurrentNamespace([]);
                return;
            }
            else
            {
                start = _elementNsStarts[index];
                length = _elementNsLengths[index];
            }

            _currentNsStart = start;
            _currentNsLength = length;
            if (start >= 0 || length == 0)
            {
                _currentNsOwned = XmlUtf8.Empty;
                return;
            }

            _currentNsOwned = _elementNsUris![index];
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

            // Every caller passes a URI that outlives the current buffer: one restored from a snapshot, or the
            // namespace of an element whose start tag was read from an earlier buffer.
            _currentNsOwned = InternUri(uri);
            _currentNsStart = -1;
            _currentNsLength = uri.Length;
        }

        /// <summary>
        /// Appends <paramref name="binding"/> to the inline or heap namespace stack.
        /// </summary>
        /// <param name="binding">The binding to store.</param>
        internal void StoreBinding(NamespaceBinding binding)
        {
            _nsPrefixCacheMask = 0;
            _nsDefaultIndex = NsIndexUnknown;
            if (_nsCount < InlineSlots)
            {
                _nsBindingsInline[_nsCount] = binding;
            }
            else
            {
                if (_nsBindings.IsEmpty)
                {
                    _nsBindings = Grow<NamespaceBinding>(default, 0, InlineSlots * 2);
                    for (var i = 0; i < InlineSlots; i++)
                    {
                        _nsBindings[i] = _nsBindingsInline[i];
                    }
                }
                else if (_nsCount == _nsBindings.Length)
                {
                    _nsBindings = Grow<NamespaceBinding>(_nsBindings, _nsCount, _nsBindings.Length * 2);
                }

                _nsBindings[_nsCount] = binding;
            }

            _nsCount++;
        }

        /// <summary>
        /// Stores the current namespace URI slots on the element stack at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The element-stack index.</param>
        /// <remarks>
        /// Sets <c>_elementNsStored</c>. From then on every start tag stores its slot, so an element
        /// in no namespace cannot inherit the slot left by an earlier element at the same depth.
        /// Documents that never use a namespace never store a slot and keep the unprefixed fast path.
        /// </remarks>
        internal void StoreElementNamespaceFromCurrent(int index)
        {
            _elementNsStored = true;
            if (index < InlineSlots)
            {
                _elementNsStartsInline[index] = _currentNsStart;
                _elementNsLengthsInline[index] = _currentNsLength;
                if (_currentNsStart < 0 && _currentNsLength > 0)
                {
                    EnsureElementNsUriCapacity(index);
                    _elementNsUris![index] = _currentNsOwned;
                }

                return;
            }

            EnsureElementNsCapacity(index);
            _elementNsStarts[index] = _currentNsStart;
            _elementNsLengths[index] = _currentNsLength;
            if (_currentNsStart < 0 && _currentNsLength > 0)
            {
                EnsureElementNsUriCapacity(index);
                _elementNsUris![index] = _currentNsOwned;
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

        #endregion

    }

}
