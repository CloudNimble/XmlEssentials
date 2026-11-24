using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CloudNimble.Text.Xml.Nodes;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Reads and writes a CLR type as attributes and child elements.
    /// </summary>
    /// <typeparam name="T">The CLR type.</typeparam>
    /// <remarks>
    /// A type with a parameterless constructor is created first and each member is set as it is read, with no
    /// boxing. A type created through a constructor with parameters holds its values until the element ends.
    /// Members are expected in the order they are written, so the member after the last one matched is tried
    /// before the rest are searched.
    /// </remarks>
    internal sealed class XmlObjectConverter<T> : XmlConverter<T>
    {

        #region Fields

        private readonly Action<Utf8XmlWriter, T, XmlSerializerOptions>? _serializeHandler;
        private readonly XmlObjectTypeInfo<T> _typeInfo;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the converter and binds the members of <typeparamref name="T"/> by reflection.
        /// </summary>
        /// <param name="options">The serializer options.</param>
        [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "The reflection resolver is not used in a trimmed application; a source-generated context supplies the binding instead.")]
        public XmlObjectConverter(XmlSerializerOptions options)
            : this(options, XmlObjectBinder.Bind<T>)
        {
        }

        /// <summary>
        /// Creates the converter with the binding of <typeparamref name="T"/> that <paramref name="bind"/> builds.
        /// </summary>
        /// <param name="options">The serializer options.</param>
        /// <param name="bind">Builds the binding: from reflection, or from metadata a source generator emitted.</param>
        /// <param name="serializeHandler">Writes the members of a value directly, or null to write them from the binding.</param>
        internal XmlObjectConverter(XmlSerializerOptions options, Func<XmlSerializerOptions, XmlObjectBinding> bind, Action<Utf8XmlWriter, T, XmlSerializerOptions>? serializeHandler = null)
        {
            _serializeHandler = serializeHandler;
            _typeInfo = new XmlObjectTypeInfo<T>(this, options, bind);
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override T? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            if (typeof(T).IsAbstract)
            {
                // The element was read as the declared type because nothing in it named a derived type.
                throw new XmlException($"The type '{typeof(T)}' is abstract or an interface, so the element must identify one of its derived types.");
            }

            return (T)ReadInto(ref reader, null, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, T value, XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(value);

            // A value type is copied wherever it is held, so it has no identity to repeat or to form a cycle with.
            if (typeof(T).IsValueType || options.ReferenceHandler is null || XmlReferenceScope.Current is not { } resolver)
            {
                WriteMembers(writer, value, options);
                return;
            }

            if (resolver is XmlIgnoreCyclesReferenceResolver cycles)
            {
                // Only an object that is still being written is a cycle. One that was written earlier and
                // finished is written again in full.
                if (!cycles.TryEnter(value))
                {
                    writer.WriteNilValue();
                    return;
                }

                WriteMembers(writer, value, options);
                cycles.Exit(value);
                return;
            }

            // The element is already open, whether it is the root, a member, or a collection item, so every
            // place an object can be written gets the same id and the same reference back to it.
            var id = resolver.GetReference(value, out var alreadyExists);
            if (alreadyExists)
            {
                WriteReference(writer, id);
                return;
            }

            if (id.Length > 0)
            {
                writer.WriteStartAttribute(null, "id", XmlNamespaces.Refs);
                writer.WriteStringValue(id);
                writer.WriteEndAttribute();
            }

            WriteMembers(writer, value, options);
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override bool CanPopulate => !typeof(T).IsAbstract;

        /// <inheritdoc />
        internal override void Populate(ref Utf8XmlReader reader, object existing, XmlSerializerOptions options)
        {
            ReadInto(ref reader, existing, options);
        }

        /// <summary>
        /// Reads the element <paramref name="reader"/> is on into a new object, or into one that already exists.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <param name="existing">The object to set the members on, boxed when it is a value type, or null to create one.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The object: <paramref name="existing"/> when one was given.</returns>
        /// <remarks>
        /// An existing object is never constructed again, so a member that only a constructor parameter can set is
        /// left as it is.
        /// </remarks>
        internal object ReadInto(ref Utf8XmlReader reader, object? existing, XmlSerializerOptions options)
        {
            var binding = _typeInfo.Binding;
            binding.ThrowIfNoMetadata(typeof(T));
            Span<bool> seen = stackalloc bool[binding.SlotCount];
            XmlObjectReadState state = default;
            state.Instance = existing ?? binding.CreateObject?.Invoke();
            if (state.Instance is null)
            {
                state.Values = binding.RentValues();
                state.Builders = state.Values;
            }

            string? referenceId = null;
            if (reader.HasAttributes)
            {
                ReadAttributes(ref reader, binding, options, ref state, seen, out referenceId, out var referenceRef);
                if (referenceRef is not null)
                {
                    if (XmlReferenceScope.Current?.ResolveReference(referenceRef) is not T resolved)
                    {
                        throw new XmlException($"Unknown reference '{referenceRef}'.");
                    }

                    if (!reader.IsEmptyElement)
                    {
                        reader.Skip();
                    }

                    if (state.Values is { } unused)
                    {
                        // A reference stands for an object that was read earlier, so the values array was not needed.
                        binding.ReturnValues(unused);
                    }

                    return resolved;
                }

                if (referenceId is not null && state.Instance is not null)
                {
                    XmlReferenceScope.Current?.AddReference(referenceId, state.Instance);
                }
            }

            if (!reader.IsEmptyElement)
            {
                if (binding.Text is null)
                {
                    ReadChildren(ref reader, binding, options, ref state, seen);
                }
                else
                {
                    ReadSimpleContent(ref reader, binding, options, ref state, seen);
                }
            }

            if (state.Builders is { } builders)
            {
                foreach (var property in binding.UnwrappedCollections)
                {
                    if (builders[property.Index] is { } builder)
                    {
                        property.StoreCollection(builder, state.Instance, state.Values);
                    }
                }
            }

            foreach (var property in binding.Required)
            {
                if (!seen[property.Index])
                {
                    throw new XmlException($"The required member '{property.Info.Name.LocalName}' is missing.");
                }
            }

            var instance = state.Instance;
            if (instance is null)
            {
                instance = Construct(binding, state.Values!, seen, options);
                binding.ReturnValues(state.Values!);
                if (referenceId is not null)
                {
                    XmlReferenceScope.Current?.AddReference(referenceId, instance);
                }
            }

            if (state.Extra is { } extra)
            {
                StoreExtensionData(binding.ExtensionData!, instance, extra);
            }

            return instance;
        }

        /// <summary>
        /// Writes, on the open start tag of an element, what stands for an object that was already written: a
        /// reference to its identifier, or nil when the resolver gives no identifier.
        /// </summary>
        /// <param name="writer">The writer, on the open start tag.</param>
        /// <param name="id">The identifier the object was written under, or an empty string.</param>
        internal static void WriteReference(Utf8XmlWriter writer, string id)
        {
            if (id.Length == 0)
            {
                writer.WriteNilValue();
                return;
            }

            writer.WriteStartAttribute(null, "ref", XmlNamespaces.Refs);
            writer.WriteStringValue(id);
            writer.WriteEndAttribute();
        }

        /// <summary>
        /// Writes the attributes and content of <paramref name="value"/> inside the element that is open.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serializer options.</param>
        internal void WriteMembers(Utf8XmlWriter writer, T value, XmlSerializerOptions options)
        {
            if (_serializeHandler is not null)
            {
                // A source generator wrote this for exactly these options, so it writes what the binding would.
                _serializeHandler(writer, value, options);
                return;
            }

            _typeInfo.Binding.ThrowIfNoMetadata(typeof(T));
            var instance = (object)value!;
            var binding = _typeInfo.Binding;
            var extra = binding.ExtensionData?.GetValue(instance);
            foreach (var property in binding.Attributes)
            {
                property.Write(writer, instance, options);
            }

            if (extra is null)
            {
                foreach (var property in binding.Content)
                {
                    property.Write(writer, instance, options);
                }

                return;
            }

            WriteExtensionAttributes(writer, extra);
            foreach (var property in binding.Content)
            {
                property.Write(writer, instance, options);
            }

            foreach (var element in extra.Elements)
            {
                element.WriteTo(writer);
            }
        }

        /// <summary>
        /// Keeps an unmapped attribute as extension data, or rejects it when unmapped members are not allowed.
        /// </summary>
        /// <param name="reader">The reader, on the attribute.</param>
        /// <param name="binding">The object binding.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="state">The object being read.</param>
        /// <exception cref="XmlException">The attribute is unmapped and unmapped members are disallowed.</exception>
        internal static void CaptureAttribute(ref Utf8XmlReader reader, XmlObjectBinding binding, XmlSerializerOptions options, ref XmlObjectReadState state)
        {
            if (binding.ExtensionData is not null)
            {
                state.Extra ??= new XmlExtensionData();
                state.Extra.Attributes[new XmlName(
                    Encoding.UTF8.GetString(reader.LocalNameSpan),
                    reader.NamespaceUriSpan.IsEmpty ? null : Encoding.UTF8.GetString(reader.NamespaceUriSpan),
                    reader.PrefixSpan.IsEmpty ? null : Encoding.UTF8.GetString(reader.PrefixSpan))] = Encoding.UTF8.GetString(reader.ValueSpan);
                return;
            }

            if (options.UnmappedMemberHandling is XmlUnmappedMemberHandling.Disallow)
            {
                throw new XmlException($"Unknown attribute '{Encoding.UTF8.GetString(reader.LocalNameSpan)}'.");
            }
        }

        /// <summary>
        /// Keeps an unmapped child element as mixed content or extension data, skips it, or rejects it when
        /// unmapped members are not allowed.
        /// </summary>
        /// <param name="reader">The reader, on the unmapped start tag.</param>
        /// <param name="binding">The object binding.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="state">The object being read.</param>
        /// <param name="seen">Whether each member has appeared, one slot per member.</param>
        /// <exception cref="XmlException">The element is unmapped and unmapped members are disallowed.</exception>
        internal static void CaptureElement(ref Utf8XmlReader reader, XmlObjectBinding binding, XmlSerializerOptions options, ref XmlObjectReadState state, scoped Span<bool> seen)
        {
            if (binding.Mixed is { } mixed)
            {
                if (XmlNode.ParseValue(ref reader) is XmlElementNode element)
                {
                    mixed.AddItem(GetBuilder(binding, mixed, ref state), new XmlMixedElement(element));
                    seen[mixed.Index] = true;
                }

                return;
            }

            if (binding.ExtensionData is not null)
            {
                state.Extra ??= new XmlExtensionData();
                if (XmlNode.ParseValue(ref reader) is XmlElementNode element)
                {
                    state.Extra.Elements.Add(element);
                }

                return;
            }

            if (options.UnmappedMemberHandling is XmlUnmappedMemberHandling.Disallow)
            {
                throw new XmlException($"Unknown element '{Encoding.UTF8.GetString(reader.LocalNameSpan)}'.");
            }

            reader.Skip();
        }

        /// <summary>
        /// Creates an instance through the constructor with parameters, passing the held values in place, then sets the held values the
        /// constructor did not take.
        /// </summary>
        /// <param name="binding">The members and constructor.</param>
        /// <param name="values">The held values, one slot per member.</param>
        /// <param name="seen">Whether each member appeared in the document.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The instance.</returns>
        /// <exception cref="XmlException">A constructor parameter with no default is missing and such parameters are required.</exception>
        internal static object Construct(XmlObjectBinding binding, object?[] values, ReadOnlySpan<bool> seen, XmlSerializerOptions options)
        {
            var properties = binding.Properties;
            var map = binding.ConstructorMap;
            for (var i = 0; i < map.Length; i++)
            {
                // A member that fills a parameter holds its value in that parameter's slot already. A second
                // parameter filled by the same member takes a copy.
                var source = map[i] >= 0 ? properties[map[i]].Index : i;
                if (map[i] >= 0 && seen[source])
                {
                    values[i] = values[source];
                    continue;
                }

                if (options.RespectRequiredConstructorParameters && !binding.ParameterHasDefault[i])
                {
                    throw new XmlException($"The required constructor parameter '{properties[map[i]].Info.Name.LocalName}' is missing.");
                }

                if (map[i] >= 0 && source == i)
                {
                    // The member's own slot may hold a box that is reused, so the default is written into it.
                    properties[map[i]].HoldDefault(values, binding.ParameterDefaults[i]);
                }
                else
                {
                    values[i] = binding.ParameterDefaults[i];
                }
            }

            var instance = binding.Constructor!(values);
            foreach (var property in properties)
            {
                if (seen[property.Index] && property.ConstructorIndex < 0 && property.CanSet)
                {
                    property.SetHeldValue(instance, values[property.Index]);
                }
            }

            return instance;
        }

        /// <inheritdoc />
        private protected override XmlTypeInfo CreateTypeInfo(XmlSerializerOptions options)
        {
            return _typeInfo;
        }

        /// <summary>
        /// Returns the member among <paramref name="attributes"/> that the reader's current attribute matches.
        /// </summary>
        /// <param name="attributes">The attribute members, in write order.</param>
        /// <param name="reader">The reader, on an attribute.</param>
        /// <param name="caseInsensitive">Whether names are compared without regard to ASCII case.</param>
        /// <param name="next">The index to try first. On a match it is moved to the member after the one found.</param>
        /// <returns>The member, or null when the attribute is unmapped.</returns>
        internal static XmlBoundProperty? FindAttribute(XmlBoundProperty[] attributes, ref Utf8XmlReader reader, bool caseInsensitive, ref int next)
        {
            var expected = next;
            if ((uint)expected < (uint)attributes.Length && attributes[expected].MatchesAttribute(ref reader, caseInsensitive))
            {
                next = expected + 1;
                return attributes[expected];
            }

            for (var i = 0; i < attributes.Length; i++)
            {
                if (i != expected && attributes[i].MatchesAttribute(ref reader, caseInsensitive))
                {
                    next = i + 1;
                    return attributes[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the member among <paramref name="elements"/> that the reader's current start tag matches.
        /// </summary>
        /// <param name="elements">The element members, in write order.</param>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <param name="caseInsensitive">Whether names are compared without regard to ASCII case.</param>
        /// <param name="next">
        /// The index to try first. On a match it is moved to the member expected next: the same member for an
        /// unwrapped collection, whose elements repeat, and the following member otherwise.
        /// </param>
        /// <returns>The member, or null when the element is unmapped.</returns>
        internal static XmlBoundProperty? FindElement(XmlBoundProperty[] elements, ref Utf8XmlReader reader, bool caseInsensitive, ref int next)
        {
            var expected = next;
            if ((uint)expected < (uint)elements.Length)
            {
                var property = elements[expected];
                if (property.Matches(ref reader, caseInsensitive))
                {
                    next = property.IsUnwrappedCollection ? expected : expected + 1;
                    return property;
                }
            }

            for (var i = 0; i < elements.Length; i++)
            {
                var property = elements[i];
                if (i != expected && property.Matches(ref reader, caseInsensitive))
                {
                    next = property.IsUnwrappedCollection ? i : i + 1;
                    return property;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the builder that gathers the children of <paramref name="property"/>, creating it on first use.
        /// </summary>
        /// <param name="binding">The object binding.</param>
        /// <param name="property">An unwrapped or mixed collection member.</param>
        /// <param name="state">The object being read.</param>
        /// <returns>The builder.</returns>
        internal static object GetBuilder(XmlObjectBinding binding, XmlBoundProperty property, ref XmlObjectReadState state)
        {
            var builders = state.Builders ??= new object?[binding.SlotCount];
            return builders[property.Index] ??= property.CreateBuilder();
        }

        /// <summary>
        /// Returns whether the current attribute is an <c>xmlns</c> declaration.
        /// </summary>
        /// <param name="reader">The reader, on an attribute.</param>
        /// <returns><see langword="true"/> for <c>xmlns</c> and <c>xmlns:*</c>.</returns>
        internal static bool IsNamespaceDeclaration(ref Utf8XmlReader reader)
        {
            return reader.PrefixSpan.SequenceEqual("xmlns"u8)
                || (reader.PrefixSpan.IsEmpty && reader.LocalNameEquals("xmlns"u8));
        }

        /// <summary>
        /// Reads the attributes of the start tag the reader is on.
        /// </summary>
        /// <param name="reader">The reader, on the object's start tag.</param>
        /// <param name="binding">The object binding.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="state">The object being read.</param>
        /// <param name="seen">Whether each member has appeared, one slot per member.</param>
        /// <param name="referenceId">When this method returns, the Preserve <c>id</c>, or null.</param>
        /// <param name="referenceRef">When this method returns, the Preserve <c>ref</c>, or null.</param>
        /// <remarks>
        /// A mapped member is looked for first. Only an attribute no member claims is checked against the names
        /// the serializer itself uses: namespace declarations, references, <c>xsi:nil</c>, <c>xsi:type</c>, and the
        /// type discriminator.
        /// </remarks>
        internal static void ReadAttributes(ref Utf8XmlReader reader, XmlObjectBinding binding, XmlSerializerOptions options, ref XmlObjectReadState state, scoped Span<bool> seen, out string? referenceId, out string? referenceRef)
        {
            referenceId = null;
            referenceRef = null;
            var attributes = binding.Attributes;
            var caseInsensitive = options.PropertyNameCaseInsensitive;
            var next = 0;
            while (reader.MoveToNextAttribute())
            {
                if (FindAttribute(attributes, ref reader, caseInsensitive, ref next) is { } property)
                {
                    property.ReadAttribute(ref reader, state.Instance, state.Values);
                    seen[property.Index] = true;
                    continue;
                }

                if (IsNamespaceDeclaration(ref reader))
                {
                    continue;
                }

                if (reader.NamespaceUriEquals(XmlNamespaces.Refs))
                {
                    if (reader.LocalNameEquals("id"u8))
                    {
                        referenceId = reader.GetString();
                    }
                    else if (reader.LocalNameEquals("ref"u8))
                    {
                        referenceRef = reader.GetString();
                    }

                    continue;
                }

                if ((reader.LocalNameEquals(XmlAttributes.Nil) || reader.LocalNameEquals(XmlAttributes.Type)) && reader.NamespaceUriEquals(XmlNamespaces.Xsi))
                {
                    continue;
                }

                if (binding.Polymorphism.Placement is XmlDiscriminatorPlacement.Attribute
                    && reader.LocalNameEquals(binding.Polymorphism.Utf8DiscriminatorAttributeName)
                    && reader.PrefixSpan.IsEmpty)
                {
                    continue;
                }

                CaptureAttribute(ref reader, binding, options, ref state);
            }

            reader.MoveToElement();
        }

        /// <summary>
        /// Reads the content of an element whose members are child elements or mixed content.
        /// </summary>
        /// <param name="reader">The reader, on the object's start tag.</param>
        /// <param name="binding">The object binding.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="state">The object being read.</param>
        /// <param name="seen">Whether each member has appeared, one slot per member.</param>
        /// <exception cref="XmlException">A member appears twice, a CDATA section has no member to hold it, or the element is not closed.</exception>
        internal static void ReadChildren(ref Utf8XmlReader reader, XmlObjectBinding binding, XmlSerializerOptions options, ref XmlObjectReadState state, scoped Span<bool> seen)
        {
            var elements = binding.Elements;
            var caseInsensitive = options.PropertyNameCaseInsensitive;
            var next = 0;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case XmlTokenType.EndElement:
                        return;
                    case XmlTokenType.StartElement:
                        if (FindElement(elements, ref reader, caseInsensitive, ref next) is not { } property)
                        {
                            CaptureElement(ref reader, binding, options, ref state, seen);
                            break;
                        }

                        if (property.IsUnwrappedCollection)
                        {
                            if (property.NeverStored && !property.Populates)
                            {
                                // Nothing can be done with the item, so it is passed over without being read.
                                reader.Skip();
                                seen[property.Index] = true;
                                break;
                            }

                            property.ReadItem(ref reader, GetBuilder(binding, property, ref state));
                            seen[property.Index] = true;
                            break;
                        }

                        if (seen[property.Index] && !options.AllowDuplicateProperties)
                        {
                            throw new XmlException($"Duplicate property '{property.Info.Name.LocalName}'.");
                        }

                        property.Read(ref reader, state.Instance, state.Values);
                        seen[property.Index] = true;
                        break;
                    case XmlTokenType.Text:
                    case XmlTokenType.CData:
                        if (binding.Mixed is { } mixed)
                        {
                            mixed.AddItem(GetBuilder(binding, mixed, ref state), new XmlMixedText(Encoding.UTF8.GetString(reader.ValueSpan)));
                            seen[mixed.Index] = true;
                        }
                        else if (reader.TokenType is XmlTokenType.CData)
                        {
                            throw new XmlException("Unexpected CData in object content.");
                        }

                        break;
                }
            }

            throw new XmlException("The element was not closed.");
        }

        /// <summary>
        /// Reads the content of an element whose text is one member's value.
        /// </summary>
        /// <param name="reader">The reader, on the object's start tag.</param>
        /// <param name="binding">The object binding.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="state">The object being read.</param>
        /// <param name="seen">Whether each member has appeared, one slot per member.</param>
        /// <exception cref="XmlException">The element is not closed.</exception>
        /// <remarks>
        /// Text that arrives in one piece with nothing to unescape is parsed where it lies in the input. Text
        /// split by a comment, a CDATA section, or a reference is joined in a rented buffer first.
        /// </remarks>
        internal static void ReadSimpleContent(ref Utf8XmlReader reader, XmlObjectBinding binding, XmlSerializerOptions options, ref XmlObjectReadState state, scoped Span<bool> seen)
        {
            var text = binding.Text!;
            byte[]? rented = null;
            try
            {
                ReadOnlySpan<byte> content = default;
                var length = 0;
                var hasText = false;
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case XmlTokenType.EndElement:
                            if (hasText)
                            {
                                text.ReadText(rented is null ? content : rented.AsSpan(0, length), state.Instance, state.Values);
                                seen[text.Index] = true;
                            }

                            return;
                        case XmlTokenType.StartElement:
                            CaptureElement(ref reader, binding, options, ref state, seen);
                            break;
                        case XmlTokenType.Text:
                        case XmlTokenType.CData:
                            if (rented is null && content.IsEmpty && (!reader.ValueIsEscaped || reader.IsEndTagNext))
                            {
                                content = reader.ValueSpan;
                            }
                            else
                            {
                                XmlConverterHelpers.CopyContent(reader.ValueSpan, ref rented, ref length, content);
                            }

                            hasText = true;
                            break;
                    }
                }

                throw new XmlException("The element was not closed.");
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        /// <summary>
        /// Gives the captured extension data to the instance: merged into the bag it already has, or set as its bag.
        /// </summary>
        /// <param name="extension">The extension-data member.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="extra">The captured attributes and elements.</param>
        internal static void StoreExtensionData(XmlExtensionDataBinding extension, object instance, XmlExtensionData extra)
        {
            if (extension.GetValue(instance) is not { } bag)
            {
                extension.SetValue(instance, extra);
                return;
            }

            foreach (var pair in extra.Attributes)
            {
                bag.Attributes[pair.Key] = pair.Value;
            }

            bag.Elements.AddRange(extra.Elements);
        }

        /// <summary>
        /// Writes unmapped attributes from <paramref name="extra"/> on the open start tag.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="extra">The bag.</param>
        internal static void WriteExtensionAttributes(Utf8XmlWriter writer, XmlExtensionData extra)
        {
            foreach (var (name, value) in extra.Attributes)
            {
                if (name.LocalName == "xmlns" || name.Prefix == "xmlns")
                {
                    continue;
                }

                XmlConverterHelpers.WriteStartAttribute(writer, name);
                writer.WriteStringValue(value);
                writer.WriteEndAttribute();
            }
        }

        #endregion

    }

}
