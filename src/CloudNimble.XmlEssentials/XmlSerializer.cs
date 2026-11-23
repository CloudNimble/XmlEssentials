using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.Text.Xml.Nodes;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Serializes CLR values to UTF-8 XML and back. Analogous to <c>JsonSerializer</c>.
    /// </summary>
    /// <remarks>
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
        /// Gets whether options with no <see cref="XmlSerializerOptions.TypeInfoResolver"/> fall back to
        /// <see cref="DefaultXmlTypeInfoResolver"/>. It is <see langword="true"/> unless the
        /// <c>CloudNimble.Text.Xml.XmlSerializer.IsReflectionEnabledByDefault</c> switch is <see langword="false"/>.
        /// </summary>
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
        internal static XmlTypeInfo GetTypeInfo(Type type, XmlSerializerOptions? options)
        {
            return (options ?? XmlSerializerOptions.Default).GetTypeInfo(type);
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
        internal static TValue? Read<TValue>(ReadOnlySpan<byte> utf8Xml, XmlTypeInfo typeInfo)
        {
            var reader = new Utf8XmlReader(utf8Xml, typeInfo.Options.ToReaderOptions());
            return ReadDocument<TValue>(ref reader, typeInfo);
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
                return Read<TValue>(rented.AsSpan(0, length), typeInfo);
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
        /// <param name="utf8Xml">The stream.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The value.</returns>
        internal static TValue? Read<TValue>(Stream utf8Xml, XmlTypeInfo typeInfo)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            var rented = XmlStreamBuffer.ReadToEnd(utf8Xml, typeInfo.Options.MaxDocumentByteCount, out var length);
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
        /// <param name="utf8Xml">The stream.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value.</returns>
        internal static async ValueTask<TValue?> ReadAsync<TValue>(Stream utf8Xml, XmlTypeInfo typeInfo, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            var utf8 = await XmlStreamBuffer.ReadToEndAsync(utf8Xml, typeInfo.Options.MaxDocumentByteCount, cancellationToken).ConfigureAwait(false);
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
        /// <param name="utf8Xml">The pipe reader.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value.</returns>
        internal static ValueTask<TValue?> ReadAsync<TValue>(PipeReader utf8Xml, XmlTypeInfo typeInfo, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            return ReadAsync<TValue>(utf8Xml.AsStream(leaveOpen: true), typeInfo, cancellationToken);
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
            if (typeInfo.Options.ReferenceHandler is { } handler)
            {
                return ReadRootWithReferences<TValue>(ref reader, typeInfo, handler);
            }

            return typeInfo is XmlTypeInfo<TValue> typed
                ? typed.ReadRoot(ref reader)
                : (TValue?)typeInfo.ReadRootAsObject(ref reader);
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
        /// <param name="values">Receives the values, in order.</param>
        /// <returns>The number of bytes used. Bytes after it belong to a value or token that is not complete yet.</returns>
        /// <remarks>
        /// One reader makes one pass. The same elements are open and the same namespaces are in scope before
        /// every child, so the state to resume from is the one captured at the document element's start tag,
        /// moved to where the last complete value ended. Tracking that costs three numbers per value; no
        /// reader is copied and no snapshot is captured per value.
        /// </remarks>
        internal static int ReadAvailableChildren<TValue>(ReadOnlySpan<byte> utf8Xml, bool isFinalBlock, XmlTypeInfo<TValue> typeInfo, ref XmlReaderState state, ref XmlReaderState scope, List<TValue?> values)
        {
            var reader = new Utf8XmlReader(utf8Xml, isFinalBlock, state);
            reader.GetResumePoint(out var start, out var lineNumber, out var lineStart);
            var end = start;
            var valueIsPartial = false;
            while (!valueIsPartial && reader.Read())
            {
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
                    valueIsPartial = !typeInfo.TryReadElement(ref reader, out var value);
                    if (valueIsPartial)
                    {
                        continue;
                    }

                    values.Add(value);
                }

                reader.GetResumePoint(out end, out lineNumber, out lineStart);
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
        /// Reads every document that is complete in <paramref name="utf8Xml"/> and reports how much of it was
        /// used. This is one step of <c>DeserializeAsyncEnumerable</c> with top-level values.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The bytes buffered so far, starting at the start of a document.</param>
        /// <param name="isFinalBlock"><see langword="true"/> when the stream has ended.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <param name="options">The reader options, with <see cref="XmlReaderOptions.AllowMultipleRoots"/> set so a tail with no element is not an error.</param>
        /// <param name="values">Receives the values, in order.</param>
        /// <returns>The number of bytes used. Bytes after it are the start of a document that is not complete yet.</returns>
        /// <remarks>
        /// Every document gets its own reader, so each may carry an XML declaration, and white space between
        /// documents is skipped here.
        /// </remarks>
        internal static int ReadAvailableDocuments<TValue>(ReadOnlySpan<byte> utf8Xml, bool isFinalBlock, XmlTypeInfo<TValue> typeInfo, XmlReaderOptions options, List<TValue?> values)
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
                if (!typeInfo.TryReadElement(ref reader, out var value))
                {
                    return used;
                }

                values.Add(value);
                used += (int)reader.BytesConsumed;
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
