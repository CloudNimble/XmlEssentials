using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.Text.Xml.Nodes;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.Text.Xml
{

    public static partial class XmlSerializer
    {

        #region Public Methods

        /// <summary>
        /// Serializes <paramref name="value"/> to an XML string.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The XML document.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static string Serialize<TValue>(TValue value, XmlSerializerOptions? options = null)
        {
            return WriteString(value, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to an XML string.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <returns>The XML document.</returns>
        public static string Serialize<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return WriteString(value, xmlTypeInfo);
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to an XML string.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The XML document.</returns>
        public static string Serialize<TValue>(TValue value, XmlSerializerContext context)
        {
            return WriteString(value, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to an XML string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The XML document.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static string Serialize(object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteString(value, GetTypeInfo(inputType, options));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to an XML string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The XML document.</returns>
        public static string Serialize(object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteString(value, GetTypeInfo(inputType, context));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to UTF-8 bytes.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The UTF-8 document.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, XmlSerializerOptions? options = null)
        {
            return WriteBytes(value, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to UTF-8 bytes.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <returns>The UTF-8 document.</returns>
        public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return WriteBytes(value, xmlTypeInfo);
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to UTF-8 bytes.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The UTF-8 document.</returns>
        public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, XmlSerializerContext context)
        {
            return WriteBytes(value, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to UTF-8 bytes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The UTF-8 document.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static byte[] SerializeToUtf8Bytes(object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteBytes(value, GetTypeInfo(inputType, options));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to UTF-8 bytes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The UTF-8 document.</returns>
        public static byte[] SerializeToUtf8Bytes(object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteBytes(value, GetTypeInfo(inputType, context));
        }

        /// <summary>
        /// Writes <paramref name="value"/> to <paramref name="writer"/> as one element.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="writer">The writer. It is not flushed.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static void Serialize<TValue>(Utf8XmlWriter writer, TValue value, XmlSerializerOptions? options = null)
        {
            WriteRoot(writer, value, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Writes <paramref name="value"/> to <paramref name="writer"/> as one element.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="writer">The writer. It is not flushed.</param>
        /// <param name="value">The value.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        public static void Serialize<TValue>(Utf8XmlWriter writer, TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            WriteRoot(writer, value, xmlTypeInfo);
        }

        /// <summary>
        /// Writes <paramref name="value"/> to <paramref name="writer"/> as one element.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="writer">The writer. It is not flushed.</param>
        /// <param name="value">The value.</param>
        /// <param name="context">The serializer context.</param>
        public static void Serialize<TValue>(Utf8XmlWriter writer, TValue value, XmlSerializerContext context)
        {
            WriteRoot(writer, value, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Writes <paramref name="value"/> as <paramref name="inputType"/> to <paramref name="writer"/> as one element.
        /// </summary>
        /// <param name="writer">The writer. It is not flushed.</param>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static void Serialize(Utf8XmlWriter writer, object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            WriteRoot(writer, value, GetTypeInfo(inputType, options));
        }

        /// <summary>
        /// Writes <paramref name="value"/> as <paramref name="inputType"/> to <paramref name="writer"/> as one element.
        /// </summary>
        /// <param name="writer">The writer. It is not flushed.</param>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">The serializer context.</param>
        public static void Serialize(Utf8XmlWriter writer, object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            WriteRoot(writer, value, GetTypeInfo(inputType, context));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to <paramref name="utf8Xml"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static void Serialize<TValue>(Stream utf8Xml, TValue value, XmlSerializerOptions? options = null)
        {
            Write(utf8Xml, value, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to <paramref name="utf8Xml"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        public static void Serialize<TValue>(Stream utf8Xml, TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            Write(utf8Xml, value, xmlTypeInfo);
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to <paramref name="utf8Xml"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="context">The serializer context.</param>
        public static void Serialize<TValue>(Stream utf8Xml, TValue value, XmlSerializerContext context)
        {
            Write(utf8Xml, value, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to <paramref name="utf8Xml"/>.
        /// </summary>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static void Serialize(Stream utf8Xml, object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            Write(utf8Xml, value, GetTypeInfo(inputType, options));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to <paramref name="utf8Xml"/>.
        /// </summary>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">The serializer context.</param>
        public static void Serialize(Stream utf8Xml, object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            Write(utf8Xml, value, GetTypeInfo(inputType, context));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to <paramref name="bufferWriter"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="bufferWriter">The destination.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static void Serialize<TValue>(IBufferWriter<byte> bufferWriter, TValue value, XmlSerializerOptions? options = null)
        {
            Write(bufferWriter, value, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to <paramref name="bufferWriter"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="bufferWriter">The destination.</param>
        /// <param name="value">The value.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        public static void Serialize<TValue>(IBufferWriter<byte> bufferWriter, TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            Write(bufferWriter, value, xmlTypeInfo);
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to <paramref name="bufferWriter"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="bufferWriter">The destination.</param>
        /// <param name="value">The value.</param>
        /// <param name="context">The serializer context.</param>
        public static void Serialize<TValue>(IBufferWriter<byte> bufferWriter, TValue value, XmlSerializerContext context)
        {
            Write(bufferWriter, value, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to <paramref name="bufferWriter"/>.
        /// </summary>
        /// <param name="bufferWriter">The destination.</param>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static void Serialize(IBufferWriter<byte> bufferWriter, object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            Write(bufferWriter, value, GetTypeInfo(inputType, options));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to <paramref name="bufferWriter"/>.
        /// </summary>
        /// <param name="bufferWriter">The destination.</param>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">The serializer context.</param>
        public static void Serialize(IBufferWriter<byte> bufferWriter, object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            Write(bufferWriter, value, GetTypeInfo(inputType, context));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to <paramref name="utf8Xml"/> without blocking on the stream.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the document has been written.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static Task SerializeAsync<TValue>(Stream utf8Xml, TValue value, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return WriteAsync(utf8Xml, value, GetTypeInfo(typeof(TValue), options), cancellationToken);
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to <paramref name="utf8Xml"/> without blocking on the stream.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the document has been written.</returns>
        public static Task SerializeAsync<TValue>(Stream utf8Xml, TValue value, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return WriteAsync(utf8Xml, value, xmlTypeInfo, cancellationToken);
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to <paramref name="utf8Xml"/> without blocking on the stream.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="context">The serializer context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the document has been written.</returns>
        public static Task SerializeAsync<TValue>(Stream utf8Xml, TValue value, XmlSerializerContext context, CancellationToken cancellationToken = default)
        {
            return WriteAsync(utf8Xml, value, GetTypeInfo(typeof(TValue), context), cancellationToken);
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to <paramref name="utf8Xml"/> without blocking on the stream.
        /// </summary>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the document has been written.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static Task SerializeAsync(Stream utf8Xml, object? value, Type inputType, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteAsync(utf8Xml, value, GetTypeInfo(inputType, options), cancellationToken);
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to <paramref name="utf8Xml"/> without blocking on the stream.
        /// </summary>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">The serializer context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the document has been written.</returns>
        public static Task SerializeAsync(Stream utf8Xml, object? value, Type inputType, XmlSerializerContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteAsync(utf8Xml, value, GetTypeInfo(inputType, context), cancellationToken);
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to a document.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The document. The caller disposes it.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static XmlDocument SerializeToDocument<TValue>(TValue value, XmlSerializerOptions? options = null)
        {
            return WriteDocument(value, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to a document.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <returns>The document. The caller disposes it.</returns>
        public static XmlDocument SerializeToDocument<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return WriteDocument(value, xmlTypeInfo);
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to a document.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The document. The caller disposes it.</returns>
        public static XmlDocument SerializeToDocument<TValue>(TValue value, XmlSerializerContext context)
        {
            return WriteDocument(value, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to a document.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The document. The caller disposes it.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static XmlDocument SerializeToDocument(object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteDocument(value, GetTypeInfo(inputType, options));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to a document.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The document. The caller disposes it.</returns>
        public static XmlDocument SerializeToDocument(object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteDocument(value, GetTypeInfo(inputType, context));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to an element.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The root element of a new document.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static XmlElement SerializeToElement<TValue>(TValue value, XmlSerializerOptions? options = null)
        {
            return WriteDocument(value, GetTypeInfo(typeof(TValue), options)).RootElement;
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to an element.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <returns>The root element of a new document.</returns>
        public static XmlElement SerializeToElement<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return WriteDocument(value, xmlTypeInfo).RootElement;
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to an element.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The root element of a new document.</returns>
        public static XmlElement SerializeToElement<TValue>(TValue value, XmlSerializerContext context)
        {
            return WriteDocument(value, GetTypeInfo(typeof(TValue), context)).RootElement;
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to an element.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The root element of a new document.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static XmlElement SerializeToElement(object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteDocument(value, GetTypeInfo(inputType, options)).RootElement;
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to an element.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The root element of a new document.</returns>
        public static XmlElement SerializeToElement(object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteDocument(value, GetTypeInfo(inputType, context)).RootElement;
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to a mutable node.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The root node.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static XmlNode? SerializeToNode<TValue>(TValue value, XmlSerializerOptions? options = null)
        {
            return WriteNode(value, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to a mutable node.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <returns>The root node.</returns>
        public static XmlNode? SerializeToNode<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return WriteNode(value, xmlTypeInfo);
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to a mutable node.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The root node.</returns>
        public static XmlNode? SerializeToNode<TValue>(TValue value, XmlSerializerContext context)
        {
            return WriteNode(value, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to a mutable node.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The root node.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static XmlNode? SerializeToNode(object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteNode(value, GetTypeInfo(inputType, options));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> as <paramref name="inputType"/> to a mutable node.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="inputType">The CLR type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The root node.</returns>
        public static XmlNode? SerializeToNode(object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteNode(value, GetTypeInfo(inputType, context));
        }

        /// <summary>
        /// Serializes <paramref name="value"/> to <paramref name="utf8Xml"/> and flushes the pipe.
        /// </summary>
        /// <remarks>
        /// To write to a pipe without flushing it, pass the <see cref="PipeWriter"/> to the
        /// <see cref="IBufferWriter{T}"/> overload of <c>Serialize</c>.
        /// </remarks>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The pipe writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the pipe has been flushed.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static async Task SerializeAsync<TValue>(PipeWriter utf8Xml, TValue value, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            Write(utf8Xml, value, GetTypeInfo(typeof(TValue), options));
            await utf8Xml.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Serializes <paramref name="values"/> as the children of one <c>ArrayOf*</c> document element.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="values">The values.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the document has been written.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static Task SerializeAsyncEnumerable<TValue>(Stream utf8Xml, IAsyncEnumerable<TValue> values, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return SerializeAsyncEnumerable(utf8Xml, values, topLevelValues: false, options, cancellationToken);
        }

        /// <summary>
        /// Serializes <paramref name="values"/> as they arrive.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="values">The values.</param>
        /// <param name="topLevelValues">When <see langword="true"/>, each value is its own document; when <see langword="false"/>, values are the children of one <c>ArrayOf*</c> document element.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when every value has been written.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static Task SerializeAsyncEnumerable<TValue>(Stream utf8Xml, IAsyncEnumerable<TValue> values, bool topLevelValues, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return SerializeAsyncEnumerable(utf8Xml, values, (options ?? XmlSerializerOptions.Default).GetTypeInfo<TValue>(), topLevelValues, cancellationToken);
        }

        /// <summary>
        /// Serializes <paramref name="values"/> as they arrive.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The destination stream.</param>
        /// <param name="values">The values.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <param name="topLevelValues">When <see langword="true"/>, each value is its own document; when <see langword="false"/>, values are the children of one <c>ArrayOf*</c> document element.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when every value has been written.</returns>
        /// <remarks>
        /// The stream is written to between values, never while one value is half written, unless a single
        /// value is larger than the writer's buffer. If a value fails to serialize, what was buffered is
        /// dropped; values already written to the stream stay there.
        /// </remarks>
        public static async Task SerializeAsyncEnumerable<TValue>(Stream utf8Xml, IAsyncEnumerable<TValue> values, XmlTypeInfo<TValue> xmlTypeInfo, bool topLevelValues = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            ArgumentNullException.ThrowIfNull(values);
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            var writer = new Utf8XmlWriter(utf8Xml, xmlTypeInfo.Options.ToWriterOptions());
            try
            {
                if (!topLevelValues)
                {
                    writer.WriteStartElement(string.Concat("ArrayOf", xmlTypeInfo.RootName.LocalName));
                }

                await foreach (var value in values.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    xmlTypeInfo.WriteRoot(writer, value);
                    if (topLevelValues)
                    {
                        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                        writer.Reset();
                    }
                    else if (writer.BytesPending > XmlLimits.DefaultBufferSize / 2)
                    {
                        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                }

                if (!topLevelValues)
                {
                    writer.WriteEndElement();
                }
            }
            catch
            {
                writer.Detach();
                throw;
            }
            finally
            {
                await writer.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Serializes <paramref name="values"/> to a pipe as they arrive, then flushes the pipe.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The pipe writer.</param>
        /// <param name="values">The values.</param>
        /// <param name="topLevelValues">When <see langword="true"/>, each value is its own document; when <see langword="false"/>, values are the children of one <c>ArrayOf*</c> document element.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the pipe has been flushed.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static async Task SerializeAsyncEnumerable<TValue>(PipeWriter utf8Xml, IAsyncEnumerable<TValue> values, bool topLevelValues, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            await SerializeAsyncEnumerable(utf8Xml.AsStream(leaveOpen: true), values, topLevelValues, options, cancellationToken).ConfigureAwait(false);
            await utf8Xml.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        #endregion

    }

}
