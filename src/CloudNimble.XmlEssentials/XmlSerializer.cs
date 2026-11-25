using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.Text.Xml.Nodes;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Provides functionality to serialize objects or value types to XML and to deserialize XML into objects or value types.
    /// This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// This class is the XML counterpart of <c>System.Text.Json.JsonSerializer</c>.
    /// Every overload resolves an <see cref="XmlTypeInfo"/> and then goes through one of the internal
    /// <c>Read</c> or <c>Write</c> methods in this file. All reading is done by <see cref="Utf8XmlReader"/> and
    /// all writing by <see cref="Utf8XmlWriter"/>.
    /// </remarks>
    public static partial class XmlSerializer
    {

        #region Fields

        /// <summary>
        /// How a DOM element or node is written before it is read back as a value: as an XML 1.1 document with
        /// its declaration.
        /// </summary>
        /// <remarks>
        /// XML 1.1 can carry every character either version allows, and the writer emits the ones 1.1
        /// restricts as character references, so content from a 1.0 or a 1.1 document reads back unchanged.
        /// </remarks>
        private static readonly XmlWriterOptions DomWriterOptions = new() { Version = XmlVersion.Version11 };

        /// <summary>
        /// The size of the stack buffer a reader is given when a document is deserialized from memory: room
        /// for a decoded value and for the decoded attribute values of one start tag.
        /// </summary>
        internal const int ReaderScratchByteCount = 512;

        /// <summary>
        /// Message for <see cref="RequiresDynamicCodeAttribute"/> on reflection overloads.
        /// </summary>
        internal const string DynMsg = "XML serialization and deserialization might require types that cannot be statically analyzed and might not be supported for trimming or AOT. Use the overload that takes an XmlTypeInfo or XmlSerializerContext, or make sure all of the required types are preserved.";

        /// <summary>
        /// Message for <see cref="RequiresUnreferencedCodeAttribute"/> on reflection overloads.
        /// </summary>
        internal const string RefMsg = "XML serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes an XmlTypeInfo or XmlSerializerContext, or make sure all of the required types are preserved.";

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets a value that indicates whether unconfigured <see cref="XmlSerializerOptions"/> instances use the
        /// reflection-based <see cref="DefaultXmlTypeInfoResolver"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if options with no <see cref="XmlSerializerOptions.TypeInfoResolver"/> fall back to
        /// <see cref="DefaultXmlTypeInfoResolver"/>; otherwise, <see langword="false"/>. The default is <see langword="true"/>.
        /// </value>
        /// <remarks>
        /// The value is <see langword="true"/> unless the
        /// <c>CloudNimble.Text.Xml.XmlSerializer.IsReflectionEnabledByDefault</c> feature switch is <see langword="false"/>.
        /// Native AOT consumers set that switch to <see langword="false"/> so the trimmer can drop the reflection resolver.
        /// </remarks>
        [FeatureSwitchDefinition("CloudNimble.Text.Xml.XmlSerializer.IsReflectionEnabledByDefault")]
        public static bool IsReflectionEnabledByDefault { get; internal set; } =
            !AppContext.TryGetSwitch("CloudNimble.Text.Xml.XmlSerializer.IsReflectionEnabledByDefault", out var enabled) || enabled;

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the type info <paramref name="options"/> has for <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The type info.</returns>
        /// <remarks>
        /// Options that have no resolver are given the reflection-based one here, on their first use by a method
        /// that takes options. Every such method says that it needs reflection.
        /// </remarks>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        internal static XmlTypeInfo GetTypeInfo(Type type, XmlSerializerOptions? options)
        {
            options ??= XmlSerializerOptions.Default;
            options.UseReflectionResolverIfMissing();
            return options.GetTypeInfo(type);
        }

        /// <summary>
        /// Returns the type info <paramref name="context"/> has for <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="context">The context.</param>
        /// <returns>The type info.</returns>
        /// <exception cref="NotSupportedException">The context has no type info for exactly <paramref name="type"/>.</exception>
        internal static XmlTypeInfo GetTypeInfo(Type type, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return context.GetTypeInfo(type) is { } typeInfo && typeInfo.Type == type
                ? typeInfo
                : throw new NotSupportedException($"The type '{type}' is not supported by '{context.GetType()}'.");
        }

        /// <summary>
        /// Reads one document.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="utf8Xml">The UTF-8 document.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The value.</returns>
        /// <remarks>
        /// The reader's scratch buffer is on the stack, so a document whose values need decoding is read
        /// without a buffer being allocated for them. It is taken in this frame and the reader is created in
        /// the next one: a reader that shares a frame with a stack buffer is copied when it is constructed,
        /// which cost 18 ns when measured.
        /// </remarks>
        [SkipLocalsInit]
        internal static TValue? Read<TValue>(ReadOnlySpan<byte> utf8Xml, XmlTypeInfo typeInfo)
        {
            Span<byte> scratch = stackalloc byte[ReaderScratchByteCount];
            return Read<TValue>(utf8Xml, scratch, typeInfo, ignoreDeclaredEncoding: false);
        }

        /// <summary>
        /// Reads one document with a reader that uses <paramref name="scratch"/> before it allocates.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="utf8Xml">The UTF-8 document.</param>
        /// <param name="scratch">Working memory for the reader.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <param name="ignoreDeclaredEncoding">Whether the bytes were transcoded from characters, so the encoding the declaration names is not checked.</param>
        /// <returns>The value.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static TValue? Read<TValue>(ReadOnlySpan<byte> utf8Xml, Span<byte> scratch, XmlTypeInfo typeInfo, bool ignoreDeclaredEncoding)
        {
            var options = typeInfo.Options.ToReaderOptions();
            options.IgnoreDeclaredEncoding = ignoreDeclaredEncoding;
            var reader = new Utf8XmlReader(utf8Xml, scratch, options);
            return ReadDocument<TValue>(ref reader, typeInfo);
        }

        /// <summary>
        /// Reads one document from UTF-8 that was transcoded from characters.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="utf8Xml">The UTF-8 document.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The value.</returns>
        /// <remarks>
        /// A declaration in the document may name the encoding the characters once had, as <c>encoding="utf-16"</c>
        /// does in what <c>System.Xml</c> writes to a string. It says nothing about these bytes and is not checked.
        /// </remarks>
        [SkipLocalsInit]
        internal static TValue? ReadTranscoded<TValue>(ReadOnlySpan<byte> utf8Xml, XmlTypeInfo typeInfo)
        {
            Span<byte> scratch = stackalloc byte[ReaderScratchByteCount];
            return Read<TValue>(utf8Xml, scratch, typeInfo, ignoreDeclaredEncoding: true);
        }

        /// <summary>
        /// Reads one document from a sequence. A sequence of several segments is copied into a pooled buffer first.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="utf8Xml">The UTF-8 document.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The value.</returns>
        internal static TValue? Read<TValue>(ReadOnlySequence<byte> utf8Xml, XmlTypeInfo typeInfo)
        {
            if (utf8Xml.IsSingleSegment)
            {
                return Read<TValue>(utf8Xml.FirstSpan, typeInfo);
            }

            var length = checked((int)utf8Xml.Length);
            var rented = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                utf8Xml.CopyTo(rented);
                return Read<TValue>(rented.AsSpan(0, length), typeInfo);
            }
            finally
            {
                XmlDocument.ReturnBuffer(rented, length);
            }
        }

        /// <summary>
        /// Reads one document from UTF-16 text by transcoding it into a pooled buffer.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="xml">The XML text.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The value.</returns>
        internal static TValue? Read<TValue>(ReadOnlySpan<char> xml, XmlTypeInfo typeInfo)
        {
            var rented = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetByteCount(xml));
            var length = 0;
            try
            {
                length = Encoding.UTF8.GetBytes(xml, rented);
                return ReadTranscoded<TValue>(rented.AsSpan(0, length), typeInfo);
            }
            finally
            {
                XmlDocument.ReturnBuffer(rented, length);
            }
        }

        /// <summary>
        /// Reads one document from a string.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="xml">The XML text.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The value.</returns>
        internal static TValue? Read<TValue>(string xml, XmlTypeInfo typeInfo)
        {
            ArgumentNullException.ThrowIfNull(xml);
            return Read<TValue>(xml.AsSpan(), typeInfo);
        }

        /// <summary>
        /// Reads one document from a stream, read to its end.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="xml">The stream.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The value.</returns>
        internal static TValue? Read<TValue>(Stream xml, XmlTypeInfo typeInfo)
        {
            ArgumentNullException.ThrowIfNull(xml);
            var rented = XmlStreamBuffer.ReadToEnd(xml,typeInfo.Options.MaxDocumentByteCount, out var length);
            try
            {
                return Read<TValue>(rented.AsSpan(0, length), typeInfo);
            }
            finally
            {
                XmlDocument.ReturnBuffer(rented, length);
            }
        }

        /// <summary>
        /// Reads the root element of <paramref name="document"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="document">The document.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The value.</returns>
        internal static TValue? Read<TValue>(XmlDocument document, XmlTypeInfo typeInfo)
        {
            ArgumentNullException.ThrowIfNull(document);
            return Read<TValue>(document.RootElement, typeInfo);
        }

        /// <summary>
        /// Reads <paramref name="element"/> by writing it to a pooled buffer, with the namespace declarations
        /// it inherits, and reading that back.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="element">The element.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The value.</returns>
        internal static TValue? Read<TValue>(XmlElement element, XmlTypeInfo typeInfo)
        {
            using var lease = new XmlWriterLease(DomWriterOptions, typeInfo.Options.DefaultBufferSize);
            element.WriteTo(lease.Writer);
            return Read<TValue>(lease.Flush(), typeInfo);
        }

        /// <summary>
        /// Reads <paramref name="node"/> by writing it to a pooled buffer and reading that back.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="node">The node.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The value.</returns>
        internal static TValue? Read<TValue>(XmlNode node, XmlTypeInfo typeInfo)
        {
            ArgumentNullException.ThrowIfNull(node);
            using var lease = new XmlWriterLease(DomWriterOptions, typeInfo.Options.DefaultBufferSize);
            node.WriteTo(lease.Writer);
            return Read<TValue>(lease.Flush(), typeInfo);
        }

        /// <summary>
        /// Reads one document from a stream without blocking on it.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="xml">The stream.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value.</returns>
        internal static async ValueTask<TValue?> ReadAsync<TValue>(Stream xml, XmlTypeInfo typeInfo, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(xml);
            var utf8 = await XmlStreamBuffer.ReadToEndAsync(xml,typeInfo.Options.MaxDocumentByteCount, cancellationToken).ConfigureAwait(false);
            try
            {
                return Read<TValue>(utf8, typeInfo);
            }
            finally
            {
                XmlDocument.ReturnBuffer(utf8.Array!, utf8.Count);
            }
        }

        /// <summary>
        /// Reads one document from a pipe without blocking on it. The pipe is read until it completes.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="xml">The pipe reader.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value.</returns>
        internal static ValueTask<TValue?> ReadAsync<TValue>(PipeReader xml, XmlTypeInfo typeInfo, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(xml);
            return ReadAsync<TValue>(xml.AsStream(leaveOpen: true), typeInfo, cancellationToken);
        }

        /// <summary>
        /// Reads the document element and then the rest of the document, so anything after the element that
        /// is not well formed is reported.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="reader">A reader at the start of a document.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The value.</returns>
        internal static TValue? ReadDocument<TValue>(ref Utf8XmlReader reader, XmlTypeInfo typeInfo)
        {
            var value = ReadRoot<TValue>(ref reader, typeInfo);
            while (reader.Read())
            {
            }

            return value;
        }

        /// <summary>
        /// Reads the next element as the document element of <paramref name="typeInfo"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The value.</returns>
        internal static TValue? ReadRoot<TValue>(ref Utf8XmlReader reader, XmlTypeInfo typeInfo)
        {
            try
            {
                if (typeInfo.Options.ReferenceHandler is { } handler)
                {
                    return ReadRootWithReferences<TValue>(ref reader, typeInfo, handler);
                }

                return typeInfo is XmlTypeInfo<TValue> typed
                    ? typed.ReadRoot(ref reader)
                    : (TValue?)typeInfo.ReadRootAsObject(ref reader);
            }
            catch (XmlException ex)
            {
                // The path of an error starts at the document element.
                reader.Locate(ex);
                ex.AddElement(typeInfo.RootName.LocalName);
                throw;
            }
        }

        /// <summary>
        /// Reads the next element as the document element of <paramref name="typeInfo"/>, resolving references
        /// between objects with a resolver that lives for this one document.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <param name="handler">The reference handler of the options.</param>
        /// <returns>The value.</returns>
        internal static TValue? ReadRootWithReferences<TValue>(ref Utf8XmlReader reader, XmlTypeInfo typeInfo, XmlReferenceHandler handler)
        {
            var previous = XmlReferenceScope.Current;
            XmlReferenceScope.Current = handler.CreateResolver();
            try
            {
                return typeInfo is XmlTypeInfo<TValue> typed
                    ? typed.ReadRoot(ref reader)
                    : (TValue?)typeInfo.ReadRootAsObject(ref reader);
            }
            finally
            {
                XmlReferenceScope.Current = previous;
            }
        }

        /// <summary>
        /// Reads every child value that is complete in <paramref name="utf8Xml"/> and reports how much of it was
        /// used. This is one step of <c>DeserializeAsyncEnumerable</c> over one document element: the caller
        /// keeps the unused tail, appends the next bytes from the stream, and calls again with the same
        /// <paramref name="state"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The bytes buffered so far, starting where the last call stopped.</param>
        /// <param name="isFinalBlock"><see langword="true"/> when the stream has ended.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <param name="state">The reader state at the start of <paramref name="utf8Xml"/>. Updated to the state at the returned offset.</param>
        /// <param name="scope">
        /// The state captured at the start tag of the document element, or a state with no snapshot until that
        /// tag has been read. Every child value starts in this scope.
        /// </param>
        /// <param name="resolver">The reference resolver shared by the values of this document, or null when references are not tracked.</param>
        /// <param name="stack">What has been read of a value the last block ended in the middle of, when the type can be read a piece at a time; otherwise null, and a value is read once all of it has arrived.</param>
        /// <param name="values">Receives the values, in order.</param>
        /// <returns>The number of bytes used. Bytes after it belong to a value or token that is not complete yet.</returns>
        /// <remarks>
        /// One reader makes one pass. The same elements are open and the same namespaces are in scope before
        /// every child, so the state to resume from is the one captured at the document element's start tag,
        /// moved to where the last complete value ended. Tracking that costs three numbers per value; no
        /// reader is copied and no snapshot is captured per value.
        /// </remarks>
        internal static int ReadAvailableChildren<TValue>(ReadOnlySpan<byte> utf8Xml, bool isFinalBlock, XmlTypeInfo<TValue> typeInfo, ref XmlReaderState state, ref XmlReaderState scope, XmlReferenceResolver? resolver, XmlReadStack? stack, List<TValue?> values)
        {
            var reader = new Utf8XmlReader(utf8Xml, isFinalBlock, state);
            reader.GetResumePoint(out var start, out var lineNumber, out var lineStart);
            var end = start;
            var valueIsPartial = false;
            if (stack is { Count: > 0 })
            {
                // The last block ended in the middle of a value. It goes on from where it stopped.
                valueIsPartial = !TryReadResumableValue(ref reader, typeInfo, stack, out var value);
                if (!valueIsPartial)
                {
                    values.Add(value);
                    reader.GetResumePoint(out end, out lineNumber, out lineStart);
                }
            }

            while (!valueIsPartial)
            {
                // Inside the document element, a run of scalar values is read an element at a time in one step.
                if (scope.Snapshot is not null && resolver is null && typeInfo.ReadWholeElements(ref reader, values) > 0)
                {
                    reader.GetResumePoint(out end, out lineNumber, out lineStart);
                }

                if (!reader.Read())
                {
                    break;
                }

                if (reader.TokenType is not XmlTokenType.StartElement)
                {
                    continue;
                }

                if (scope.Snapshot is null)
                {
                    scope = reader.CurrentState;
                }
                else
                {
                    // A value that can be read a piece at a time is read as far as the block goes. Any other, and
                    // one marked nil, is read once all of it has arrived.
                    valueIsPartial = stack is null || typeInfo.IsNil(ref reader)
                        ? !TryReadStreamedValue(ref reader, typeInfo, resolver, out var value)
                        : !TryReadResumableValue(ref reader, typeInfo, stack, out value);
                    if (valueIsPartial)
                    {
                        continue;
                    }

                    values.Add(value);
                }

                reader.GetResumePoint(out end, out lineNumber, out lineStart);
            }

            if (stack is { Count: > 0 })
            {
                // The block ended inside a value, and the next one starts there: the elements that are open and
                // the namespaces in scope are those of the middle of the value, so the state is the reader's own.
                reader.GetResumePoint(out end, out _, out _);
                state = end > start ? reader.CurrentState : state;
                return (int)(end - start);
            }

            if (scope.Snapshot is null)
            {
                // Still before the document element: the reader stopped between two tokens of the prolog.
                reader.GetResumePoint(out end, out _, out _);
                state = end > start ? reader.CurrentState : state;
            }
            else if (end > start)
            {
                state = new XmlReaderState(scope.Options, scope.Snapshot.At(end, lineNumber, lineStart));
            }

            return (int)(end - start);
        }

        /// <summary>
        /// Reads one value of an asynchronous sequence as far as the block goes, or goes on with the one
        /// <paramref name="stack"/> holds.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="reader">The reader: on the start tag of the value, or where the last block ended inside it.</param>
        /// <param name="typeInfo">The type info, whose object can be read a piece at a time.</param>
        /// <param name="stack">What has been read of the value so far.</param>
        /// <param name="value">When this method returns true, the value.</param>
        /// <returns><see langword="false"/> if the end of the value is not in the block. The stack keeps what was read.</returns>
        internal static bool TryReadResumableValue<TValue>(ref Utf8XmlReader reader, XmlTypeInfo<TValue> typeInfo, XmlReadStack stack, out TValue? value)
        {
            try
            {
                var complete = XmlResumableReader.TryRead(ref reader, stack, typeInfo.ResumableBinding!, typeInfo.Type, typeInfo.Options, out var read);
                value = complete ? (TValue?)read : default;
                return complete;
            }
            catch (XmlException ex)
            {
                // The path of an error in a value that is read from a stream starts at the element of the value.
                reader.Locate(ex);
                ex.AddElement(typeInfo.RootName.LocalName);
                throw;
            }
        }

        /// <summary>
        /// Reads every document that is complete in <paramref name="utf8Xml"/> and reports how much of it was
        /// used. This is one step of <c>DeserializeAsyncEnumerable</c> with top-level values.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The bytes buffered so far, starting at the start of a document.</param>
        /// <param name="isFinalBlock"><see langword="true"/> when the stream has ended.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <param name="options">The reader options, with <see cref="XmlReaderOptions.AllowMultipleRoots"/> set so a tail with no element is not an error.</param>
        /// <param name="handler">The reference handler, which creates a resolver for each document, or null when references are not tracked.</param>
        /// <param name="values">Receives the values, in order.</param>
        /// <returns>The number of bytes used. Bytes after it are the start of a document that is not complete yet.</returns>
        /// <remarks>
        /// Every document gets its own reader, so each may carry an XML declaration, and white space between
        /// documents is skipped here.
        /// </remarks>
        internal static int ReadAvailableDocuments<TValue>(ReadOnlySpan<byte> utf8Xml, bool isFinalBlock, XmlTypeInfo<TValue> typeInfo, XmlReaderOptions options, XmlReferenceHandler? handler, List<TValue?> values)
        {
            var used = 0;
            while (true)
            {
                used = utf8Xml.Length - utf8Xml[used..].TrimStart(" \t\r\n"u8).Length;
                var reader = new Utf8XmlReader(utf8Xml[used..], isFinalBlock, new XmlReaderState(options));
                if (!XmlConverterHelpers.TryMoveToStartElement(ref reader))
                {
                    return used;
                }

                typeInfo.CheckRoot(ref reader);
                if (!TryReadStreamedValue(ref reader, typeInfo, handler?.CreateResolver(), out var value))
                {
                    return used;
                }

                values.Add(value);
                used += (int)reader.BytesConsumed;
            }
        }

        /// <summary>
        /// Reads one value of an asynchronous sequence if all of it has arrived, tracking references with
        /// <paramref name="resolver"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="reader">The reader, on the start tag of the value.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <param name="resolver">The resolver for the document the value belongs to, or null when references are not tracked.</param>
        /// <param name="value">When this method returns true, the value.</param>
        /// <returns><see langword="false"/> if the end of the value is not in the buffer yet.</returns>
        /// <remarks>
        /// An object is read only once all of it has arrived, so the resolver sees each identifier once
        /// however the stream is cut up. The resolver is current only during the call.
        /// </remarks>
        internal static bool TryReadStreamedValue<TValue>(ref Utf8XmlReader reader, XmlTypeInfo<TValue> typeInfo, XmlReferenceResolver? resolver, out TValue? value)
        {
            // With no reference handler
            // the resolver is the null the scope holds already, so nothing is set for each value.
            var previous = XmlReferenceScope.Current;
            var scoped = !ReferenceEquals(previous, resolver);
            if (scoped)
            {
                XmlReferenceScope.Current = resolver;
            }

            try
            {
                return typeInfo.TryReadElement(ref reader, out value);
            }
            catch (XmlException ex)
            {
                // The path of an error in a value that is read from a stream starts at the element of the value.
                reader.Locate(ex);
                ex.AddElement(typeInfo.RootName.LocalName);
                throw;
            }
            finally
            {
                if (scoped)
                {
                    XmlReferenceScope.Current = previous;
                }
            }
        }

        /// <summary>
        /// Copies serializer options onto document options.
        /// </summary>
        /// <param name="options">The serializer options.</param>
        /// <returns>Document options.</returns>
        internal static XmlDocumentOptions ToDocumentOptions(XmlSerializerOptions options)
        {
            return new XmlDocumentOptions
            {
                CommentHandling = options.ReadCommentHandling,
                MaxAttributeCount = options.MaxAttributeCount,
                MaxCommentByteCount = options.MaxCommentByteCount,
                MaxDepth = options.MaxDepth,
                MaxDocumentByteCount = options.MaxDocumentByteCount,
                MaxNameByteCount = options.MaxNameByteCount,
                MaxTextByteCount = options.MaxTextByteCount,
                WhitespaceHandling = options.WhitespaceHandling,
            };
        }

        /// <summary>
        /// Writes one document to <paramref name="bufferWriter"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="bufferWriter">The destination. If the write fails, bytes not yet committed to it are dropped.</param>
        /// <param name="value">The value.</param>
        /// <param name="typeInfo">The type info.</param>
        internal static void Write<TValue>(IBufferWriter<byte> bufferWriter, TValue value, XmlTypeInfo typeInfo)
        {
            ArgumentNullException.ThrowIfNull(bufferWriter);
            using var lease = new XmlWriterLease(typeInfo.Options.ToWriterOptions(), bufferWriter);
            WriteRoot(lease.Writer, value, typeInfo);
            lease.Writer.Flush();
        }

        /// <summary>
        /// Writes one document to <paramref name="utf8Xml"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="typeInfo">The type info.</param>
        internal static void Write<TValue>(Stream utf8Xml, TValue value, XmlTypeInfo typeInfo)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            using var lease = new XmlWriterLease(typeInfo.Options.ToWriterOptions(), utf8Xml);
            WriteRoot(lease.Writer, value, typeInfo);
            lease.Writer.Flush();
        }

        /// <summary>
        /// Writes one document to a pooled buffer and then to <paramref name="utf8Xml"/> in a single
        /// asynchronous write.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the document has been written.</returns>
        internal static async Task WriteAsync<TValue>(Stream utf8Xml, TValue value, XmlTypeInfo typeInfo, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            var buffer = new XmlPooledBufferWriter();
            buffer.Rent(XmlLimits.Resolve(typeInfo.Options.DefaultBufferSize, XmlLimits.DefaultBufferSize));
            try
            {
                Write(buffer, value, typeInfo);
                await utf8Xml.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                buffer.Return();
            }
        }

        /// <summary>
        /// Writes one document and returns it as an array.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The UTF-8 document.</returns>
        internal static byte[] WriteBytes<TValue>(TValue value, XmlTypeInfo typeInfo)
        {
            using var lease = new XmlWriterLease(typeInfo.Options.ToWriterOptions(), typeInfo.Options.DefaultBufferSize);
            WriteRoot(lease.Writer, value, typeInfo);
            return lease.Flush().ToArray();
        }

        /// <summary>
        /// Writes one document and parses it into an <see cref="XmlDocument"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The document.</returns>
        internal static XmlDocument WriteDocument<TValue>(TValue value, XmlTypeInfo typeInfo)
        {
            using var lease = new XmlWriterLease(typeInfo.Options.ToWriterOptions(), typeInfo.Options.DefaultBufferSize);
            WriteRoot(lease.Writer, value, typeInfo);
            return XmlDocument.Parse(lease.Flush(), ToDocumentOptions(typeInfo.Options));
        }

        /// <summary>
        /// Writes one document and returns its root element, in a document that holds no pooled memory.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The root element.</returns>
        /// <remarks>
        /// The caller gets an element and has no document to dispose, so the document behind it gives its
        /// rented arrays back here and keeps copies of its own.
        /// </remarks>
        internal static XmlElement WriteElement<TValue>(TValue value, XmlTypeInfo typeInfo)
        {
            var document = WriteDocument(value, typeInfo);
            document.MoveOutOfPool();
            return document.RootElement;
        }

        /// <summary>
        /// Writes one document and parses it into a mutable <see cref="XmlNode"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The root node.</returns>
        internal static XmlNode? WriteNode<TValue>(TValue value, XmlTypeInfo typeInfo)
        {
            using var lease = new XmlWriterLease(typeInfo.Options.ToWriterOptions(), typeInfo.Options.DefaultBufferSize);
            WriteRoot(lease.Writer, value, typeInfo);
            return XmlNode.Parse(lease.Flush());
        }

        /// <summary>
        /// Writes <paramref name="value"/> to <paramref name="writer"/> as the document element of <paramref name="typeInfo"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="typeInfo">The type info.</param>
        internal static void WriteRoot<TValue>(Utf8XmlWriter writer, TValue value, XmlTypeInfo typeInfo)
        {
            ArgumentNullException.ThrowIfNull(writer);
            try
            {
                if (typeInfo.Options.ReferenceHandler is { } handler)
                {
                    WriteRootWithReferences(writer, value, typeInfo, handler);
                    return;
                }

                if (typeInfo is XmlTypeInfo<TValue> typed)
                {
                    typed.WriteRoot(writer, value);
                    return;
                }

                typeInfo.WriteRootAsObject(writer, value);
            }
            catch (XmlException ex)
            {
                // The path of an error is the elements the writer has open.
                ex.SetPathIfMissing(writer.CurrentPath());
                throw;
            }
        }

        /// <summary>
        /// Writes one value of an asynchronous sequence, tracking references with <paramref name="resolver"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <param name="resolver">The resolver for the document the value belongs to.</param>
        /// <remarks>
        /// The resolver is current only while the value is written. Nothing is awaited in between, so it is
        /// not seen by the code that produces the next value or by the stream. With no reference handler nothing
        /// that is written asks for a resolver, and the caller writes the value directly.
        /// </remarks>
        internal static void WriteStreamedValue<TValue>(Utf8XmlWriter writer, TValue value, XmlTypeInfo<TValue> typeInfo, XmlReferenceResolver resolver)
        {
            var previous = XmlReferenceScope.Current;
            XmlReferenceScope.Current = resolver;
            try
            {
                typeInfo.WriteRoot(writer, value);
            }
            finally
            {
                XmlReferenceScope.Current = previous;
            }
        }

        /// <summary>
        /// Writes <paramref name="value"/> as the document element of <paramref name="typeInfo"/>, tracking references
        /// between objects with a resolver that lives for this one document.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <param name="handler">The reference handler of the options.</param>
        internal static void WriteRootWithReferences<TValue>(Utf8XmlWriter writer, TValue value, XmlTypeInfo typeInfo, XmlReferenceHandler handler)
        {
            var previous = XmlReferenceScope.Current;
            XmlReferenceScope.Current = handler.CreateResolver();
            try
            {
                if (typeInfo is XmlTypeInfo<TValue> typed)
                {
                    typed.WriteRoot(writer, value);
                    return;
                }

                typeInfo.WriteRootAsObject(writer, value);
            }
            finally
            {
                XmlReferenceScope.Current = previous;
            }
        }

        /// <summary>
        /// Writes one document and returns it as a string.
        /// </summary>
        /// <typeparam name="TValue">The CLR type, or <see cref="object"/> when only <paramref name="typeInfo"/> knows it.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The XML document.</returns>
        internal static string WriteString<TValue>(TValue value, XmlTypeInfo typeInfo)
        {
            using var lease = new XmlWriterLease(typeInfo.Options.ToWriterOptions(), typeInfo.Options.DefaultBufferSize);
            WriteRoot(lease.Writer, value, typeInfo);
            return Encoding.UTF8.GetString(lease.Flush());
        }

        #endregion

    }

}
