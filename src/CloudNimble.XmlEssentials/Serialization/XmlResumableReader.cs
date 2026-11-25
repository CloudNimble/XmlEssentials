using System;
using System.Collections;
using System.Text;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Reads an object from a stream in as many pieces as the stream delivers it in, and goes on where it stopped.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the design System.Text.Json has for its object and collection converters. A value keeps what
    /// it has read so far in the frames of an <see cref="XmlReadStack"/>, so when a block of input ends in the
    /// middle of it nothing is read again and no setter or constructor runs twice. A value that is all in one
    /// block is read without the stack ever holding more than its own frames.
    /// </para>
    /// <para>
    /// It reads what <see cref="XmlObjectBinding.IsResumable()"/> allows: objects the built-in converter reads,
    /// whose members are built-in scalars, such objects, or collections of either. Anything else is read as it
    /// always was, after the whole value has been shown to be in the block, which is also what System.Text.Json
    /// does for a converter that cannot stop part way. The members, the checks and the messages are those of
    /// <see cref="XmlObjectConverter{T}"/>, whose own methods this calls wherever a step is not split.
    /// </para>
    /// </remarks>
    internal static class XmlResumableReader
    {

        #region Private Methods

        /// <summary>
        /// Adds to the path of an error the member or the item that <paramref name="frame"/> was reading.
        /// </summary>
        /// <param name="frame">A frame of the value that could not be read.</param>
        /// <param name="exception">The error.</param>
        /// <remarks>
        /// The frames are asked from the innermost out, which is the order the recursive read adds the same
        /// names in as the exception passes through it.
        /// </remarks>
        internal static void AddPath(XmlReadFrame frame, XmlException exception)
        {
            if (frame.Wrapper is { } wrapper)
            {
                // An item of a collection in a wrapper. Its position is the number of items read before it.
                exception.AddItem(Encoding.UTF8.GetString(wrapper.Utf8ItemName), ((ICollection)frame.WrapperBuilder!).Count);
                return;
            }

            if (frame.State.Current is not { } member)
            {
                return;
            }

            if (member.IsUnwrappedCollection)
            {
                var builder = XmlObjectConverter<object>.GetBuilder(frame.Binding!, member, ref frame.State);
                exception.AddItem(member.Info.Name.LocalName, ((ICollection)builder).Count);
                return;
            }

            exception.AddElement(member.Info.Name.LocalName);
        }

        /// <summary>
        /// Starts reading an item of a collection from the start tag the reader is on.
        /// </summary>
        /// <param name="reader">The reader, on the start tag of the item.</param>
        /// <param name="stack">The stack.</param>
        /// <param name="frame">The frame the item is read in.</param>
        /// <param name="collection">The collection.</param>
        /// <param name="builder">The builder of the collection.</param>
        internal static void BeginItem(ref Utf8XmlReader reader, XmlReadStack stack, XmlReadFrame frame, XmlCollectionBinding collection, object builder)
        {
            frame.ElementCollection = collection;
            frame.ElementBuilder = builder;
            if (collection.ItemTypeInfo.IsNilElement(ref reader))
            {
                BeginSkip(ref reader, frame, isNil: true);
                return;
            }

            if (collection.HasItemValueConverter)
            {
                BeginLeaf(ref reader, frame);
                return;
            }

            var item = stack.Push();
            item.Binding = collection.ItemTypeInfo.ResumableBinding;
            item.Type = collection.ItemTypeInfo.Type;
        }

        /// <summary>
        /// Starts reading the text of an element that holds one value, and finishes at once when the text and the
        /// end tag are plain and in the block.
        /// </summary>
        /// <param name="reader">The reader, on the start tag.</param>
        /// <param name="frame">The frame, which says where the value goes.</param>
        internal static void BeginLeaf(ref Utf8XmlReader reader, XmlReadFrame frame)
        {
            if (reader.IsEmptyElement)
            {
                EndLeaf(frame, []);
                return;
            }

            if (reader.TryReadLeafContent(out var content))
            {
                EndLeaf(frame, content);
                return;
            }

            // The text is in pieces, needs decoding, or is not all here: it is gathered token by token.
            frame.LeafLength = 0;
            frame.Pending = XmlReadPending.Leaf;
        }

        /// <summary>
        /// Starts reading a member of an object from the start tag the reader is on.
        /// </summary>
        /// <param name="reader">The reader, on the start tag of a child element.</param>
        /// <param name="stack">The stack.</param>
        /// <param name="frame">The frame of the object.</param>
        /// <param name="options">The serializer options.</param>
        /// <exception cref="XmlException">The element is unknown and unknown members are refused, or the member has appeared already.</exception>
        internal static void BeginMember(ref Utf8XmlReader reader, XmlReadStack stack, XmlReadFrame frame, XmlSerializerOptions options)
        {
            var binding = frame.Binding!;
            var property = XmlObjectConverter<object>.FindElement(binding.Elements, ref reader, options.PropertyNameCaseInsensitive, ref frame.Next);
            if (property is null)
            {
                if (options.UnmappedMemberHandling is XmlUnmappedMemberHandling.Disallow)
                {
                    throw XmlException.Create($"Unknown element '{Encoding.UTF8.GetString(reader.LocalNameSpan)}'.");
                }

                BeginSkip(ref reader, frame, isNil: false);
                return;
            }

            if (property.IsUnwrappedCollection)
            {
                if (property.NeverStored)
                {
                    // Nothing can be done with the item, so it is passed over without being read.
                    frame.ElementProperty = property;
                    BeginSkip(ref reader, frame, isNil: false);
                    return;
                }

                frame.State.Current = property;
                BeginItem(ref reader, stack, frame, property.Collection!, XmlObjectConverter<object>.GetBuilder(binding, property, ref frame.State));
                return;
            }

            if (frame.Seen[property.Index] && !options.AllowDuplicateProperties)
            {
                throw XmlException.Create($"Duplicate property '{property.Info.Name.LocalName}'.");
            }

            frame.State.Current = property;
            frame.ElementProperty = property;
            if (property.NeverStored)
            {
                BeginSkip(ref reader, frame, isNil: false);
                return;
            }

            if (property.Collection is { } wrapped)
            {
                if (reader.HasAttributes && reader.GetNil())
                {
                    // A wrapper marked nil is a null collection, which is not the same as an empty one.
                    BeginSkip(ref reader, frame, isNil: true);
                    return;
                }

                var wrapper = stack.Push();
                wrapper.Wrapper = wrapped;
                wrapper.WrapperBuilder = wrapped.CreateBuilder();
                return;
            }

            if (property.ValueTypeInfo.IsNilElement(ref reader))
            {
                BeginSkip(ref reader, frame, isNil: true);
                return;
            }

            if (property.HasValueConverter)
            {
                BeginLeaf(ref reader, frame);
                return;
            }

            var child = stack.Push();
            child.Binding = property.ValueTypeInfo.ResumableBinding;
            child.Type = property.ValueTypeInfo.Type;
        }

        /// <summary>
        /// Starts passing over the element the reader is on.
        /// </summary>
        /// <param name="reader">The reader, on the start tag.</param>
        /// <param name="frame">The frame.</param>
        /// <param name="isNil">Whether the element was marked <c>xsi:nil</c>, so that null is stored when it has ended.</param>
        internal static void BeginSkip(ref Utf8XmlReader reader, XmlReadFrame frame, bool isNil)
        {
            frame.ElementIsNil = isNil;
            if (reader.IsEmptyElement)
            {
                EndSkip(frame);
                return;
            }

            frame.SkipDepth = reader.CurrentDepth;
            frame.Pending = XmlReadPending.Skip;
        }

        /// <summary>
        /// Notes that the element <paramref name="frame"/> was reading has been read.
        /// </summary>
        /// <param name="frame">The frame.</param>
        internal static void EndElement(XmlReadFrame frame)
        {
            if (frame.Binding is not null && (frame.State.Current ?? frame.ElementProperty) is { } member)
            {
                frame.Seen[member.Index] = true;
            }

            frame.State.Current = null;
            frame.ElementBuilder = null;
            frame.ElementCollection = null;
            frame.ElementIsNil = false;
            frame.ElementProperty = null;
            frame.Pending = XmlReadPending.None;
        }

        /// <summary>
        /// Gives the text of an element that holds one value to the member or the collection it belongs to.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="content">The text.</param>
        internal static void EndLeaf(XmlReadFrame frame, ReadOnlySpan<byte> content)
        {
            if (frame.ElementCollection is { } collection)
            {
                collection.AddLexicalItem(frame.ElementBuilder!, content);
            }
            else
            {
                frame.ElementProperty!.ReadText(content, frame.State.Instance, frame.State.Values);
            }

            EndElement(frame);
        }

        /// <summary>
        /// Stores null for an element that was marked nil, once the element has been passed over.
        /// </summary>
        /// <param name="frame">The frame.</param>
        internal static void EndSkip(XmlReadFrame frame)
        {
            if (frame.ElementIsNil)
            {
                if (frame.ElementCollection is { } collection)
                {
                    collection.AddNilItem(frame.ElementBuilder!);
                }
                else
                {
                    frame.ElementProperty!.ReadNil(frame.State.Instance, frame.State.Values);
                }
            }

            EndElement(frame);
        }

        /// <summary>
        /// Makes the value of a frame whose end tag has been read.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The object, or the collection.</returns>
        /// <exception cref="XmlException">A required member is missing.</exception>
        internal static object Finish(XmlReadFrame frame, XmlSerializerOptions options)
        {
            if (frame.Wrapper is { } wrapper)
            {
                return wrapper.Finish(frame.WrapperBuilder!);
            }

            var binding = frame.Binding!;
            if (frame.State.Builders is { } builders)
            {
                foreach (var property in binding.UnwrappedCollections)
                {
                    if (builders[property.Index] is { } builder)
                    {
                        property.StoreCollection(builder, frame.State.Instance, frame.State.Values);
                    }
                }
            }

            foreach (var property in binding.Required)
            {
                if (!frame.Seen[property.Index])
                {
                    throw XmlException.Create($"The required member '{property.Info.Name.LocalName}' is missing.");
                }
            }

            if (frame.State.Instance is { } instance)
            {
                return instance;
            }

            var values = frame.State.Values!;
            var constructed = XmlObjectConverter<object>.Construct(binding, values, frame.Seen.AsSpan(0, binding.SlotCount), options);
            binding.ReturnValues(values);
            return constructed;
        }

        /// <summary>
        /// Reads the start tag of an object: makes the instance, or the place its constructor arguments are
        /// held, and reads the attributes.
        /// </summary>
        /// <param name="reader">The reader, on the start tag.</param>
        /// <param name="frame">The frame.</param>
        /// <param name="options">The serializer options.</param>
        /// <exception cref="XmlException">The start tag refers to another object, which a stream read with no reference handler cannot find.</exception>
        internal static void Start(ref Utf8XmlReader reader, XmlReadFrame frame, XmlSerializerOptions options)
        {
            frame.Stage = reader.IsEmptyElement ? XmlReadStage.End : XmlReadStage.Content;
            if (frame.Binding is not { } binding)
            {
                return;
            }

            if (frame.Seen.Length < binding.SlotCount)
            {
                frame.Seen = new bool[binding.SlotCount];
            }
            else
            {
                frame.Seen.AsSpan(0, binding.SlotCount).Clear();
            }

            frame.State.Instance = binding.CreateObject?.Invoke();
            if (frame.State.Instance is null)
            {
                frame.State.Values = binding.RentValues();
                frame.State.Builders = frame.State.Values;
            }

            if (!reader.HasAttributes)
            {
                return;
            }

            XmlObjectConverter<object>.ReadAttributes(ref reader, binding, options, ref frame.State, frame.Seen.AsSpan(0, binding.SlotCount), out _, out var reference);
            if (reference is not null)
            {
                // No resolver is current when this reads: a stream with a reference handler is not read this way.
                throw XmlException.Create($"Unknown reference '{reference}'.");
            }
        }

        /// <summary>
        /// Goes on with the element a frame was in the middle of: the text of a value, or an element that is
        /// passed over.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="frame">The frame.</param>
        /// <returns><see langword="false"/> when the block ended before the element did.</returns>
        /// <exception cref="XmlException">The element that holds one value has a child element.</exception>
        internal static bool TryFinishElement(ref Utf8XmlReader reader, XmlReadFrame frame)
        {
            if (frame.Pending is XmlReadPending.Skip)
            {
                while (reader.Read())
                {
                    if (reader.TokenType is XmlTokenType.EndElement && reader.CurrentDepth == frame.SkipDepth)
                    {
                        EndSkip(frame);
                        return true;
                    }
                }

                return false;
            }

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case XmlTokenType.EndElement:
                        EndLeaf(frame, frame.Leaf.AsSpan(0, frame.LeafLength));
                        return true;
                    case XmlTokenType.Text:
                    case XmlTokenType.CData:
                        // The text is copied, because the block it is in may be gone before the end tag arrives.
                        var value = reader.ValueSpan;
                        if (frame.Leaf.Length - frame.LeafLength < value.Length)
                        {
                            var grown = new byte[Math.Max(frame.Leaf.Length * 2, frame.LeafLength + value.Length)];
                            frame.Leaf.AsSpan(0, frame.LeafLength).CopyTo(grown);
                            frame.Leaf = grown;
                        }

                        value.CopyTo(frame.Leaf.AsSpan(frame.LeafLength));
                        frame.LeafLength += value.Length;
                        break;
                    case XmlTokenType.Comment:
                    case XmlTokenType.ProcessingInstruction:
                        break;
                    default:
                        throw XmlException.Create($"Unexpected {reader.TokenType} in simple content.");
                }
            }

            return false;
        }

        /// <summary>
        /// Reads a value, or goes on reading the one the stack holds.
        /// </summary>
        /// <param name="reader">
        /// The reader: on the start tag of the value when <paramref name="stack"/> is empty, and otherwise where
        /// the last block ended.
        /// </param>
        /// <param name="stack">The stack. It is empty when this method returns <see langword="true"/> or throws.</param>
        /// <param name="binding">The binding of the type of the value.</param>
        /// <param name="type">The type of the value.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="value">When this method returns <see langword="true"/>, the value.</param>
        /// <returns>
        /// <see langword="false"/> when the block ended before the value did. The stack then holds what was read,
        /// and the next call goes on with a reader that starts where this one stopped.
        /// </returns>
        internal static bool TryRead(ref Utf8XmlReader reader, XmlReadStack stack, XmlObjectBinding binding, Type type, XmlSerializerOptions options, out object? value)
        {
            value = null;
            try
            {
                if (stack.Count == 0)
                {
                    var root = stack.Push();
                    root.Binding = binding;
                    root.Type = type;
                }

                while (true)
                {
                    var frame = stack.Top;
                    if (frame.Pending is not XmlReadPending.None && !TryFinishElement(ref reader, frame))
                    {
                        return false;
                    }

                    if (frame.Stage is XmlReadStage.Start)
                    {
                        Start(ref reader, frame, options);
                    }

                    if (frame.Stage is XmlReadStage.Content)
                    {
                        if (!TryReadContent(ref reader, stack, frame, options))
                        {
                            return false;
                        }

                        if (!ReferenceEquals(stack.Top, frame))
                        {
                            // A member or an item has a frame of its own, which is read first.
                            continue;
                        }
                    }

                    var finished = Finish(frame, options);
                    stack.Pop();
                    if (stack.Count == 0)
                    {
                        value = finished;
                        return true;
                    }

                    var parent = stack.Top;
                    if (parent.ElementCollection is { } collection)
                    {
                        collection.AddItem(parent.ElementBuilder!, finished);
                    }
                    else
                    {
                        parent.ElementProperty!.StoreValue(parent.State.Instance, parent.State.Values, finished);
                    }

                    EndElement(parent);
                }
            }
            catch (XmlException exception)
            {
                // The path of the error is the member or the item each frame was reading, from the innermost out.
                reader.Locate(exception);
                for (var i = stack.Count - 1; i >= 0; i--)
                {
                    AddPath(stack[i], exception);
                }

                stack.Clear();
                throw;
            }
            catch
            {
                stack.Clear();
                throw;
            }
        }

        /// <summary>
        /// Reads the child elements of a frame until one needs a frame of its own, or the end tag is read.
        /// </summary>
        /// <param name="reader">The reader, inside the element.</param>
        /// <param name="stack">The stack.</param>
        /// <param name="frame">The frame.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns><see langword="false"/> when the block ended first.</returns>
        /// <exception cref="XmlException">An object holds a CDATA section, which no member takes.</exception>
        internal static bool TryReadContent(ref Utf8XmlReader reader, XmlReadStack stack, XmlReadFrame frame, XmlSerializerOptions options)
        {
            while (true)
            {
                // An element that holds one scalar, and is the member expected next, is read whole when all
                // of it is in the block. A binding that is read here has no mixed content.
                if (frame.Binding is { } binding
                    && (uint)frame.Next < (uint)binding.Elements.Length
                    && binding.Elements[frame.Next] is { ReadsWholeLeaf: true, NeverStored: false, SiblingNamespaces: null, LeafOpenTag: { } openTag } expected
                    && !frame.Seen[expected.Index]
                    && reader.TryReadExpectedLeaf(openTag, expected.LeafCloseTag, out var content))
                {
                    frame.State.Current = expected;
                    frame.ElementProperty = expected;
                    frame.Next++;
                    EndLeaf(frame, content);
                    continue;
                }

                if (!reader.Read())
                {
                    break;
                }

                switch (reader.TokenType)
                {
                    case XmlTokenType.EndElement:
                        frame.Stage = XmlReadStage.End;
                        return true;
                    case XmlTokenType.StartElement:
                        if (frame.Wrapper is { } wrapper)
                        {
                            BeginItem(ref reader, stack, frame, wrapper, frame.WrapperBuilder!);
                        }
                        else
                        {
                            BeginMember(ref reader, stack, frame, options);
                        }

                        if (!ReferenceEquals(stack.Top, frame))
                        {
                            return true;
                        }

                        if (frame.Pending is not XmlReadPending.None && !TryFinishElement(ref reader, frame))
                        {
                            return false;
                        }

                        break;
                    case XmlTokenType.CData:
                        if (frame.Wrapper is null)
                        {
                            throw XmlException.Create("Unexpected CData in object content.");
                        }

                        break;
                }
            }

            return false;
        }

        #endregion

    }

}
