using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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
        /// Deserializes XML text as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="xml">The XML text.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(string xml, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(xml, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Deserializes XML text as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="xml">The XML text.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(string xml, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(xml, xmlTypeInfo);
        }

        /// <summary>
        /// Deserializes XML text as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="xml">The XML text.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(string xml, XmlSerializerContext context)
        {
            return Read<TValue>(xml, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Deserializes XML text as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="xml">The XML text.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(string xml, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(xml, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Deserializes XML text as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="xml">The XML text.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static object? Deserialize(string xml, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(xml, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Deserializes UTF-16 XML text as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="xml">The XML text.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(ReadOnlySpan<char> xml, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(xml, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Deserializes UTF-16 XML text as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="xml">The XML text.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(ReadOnlySpan<char> xml, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(xml, xmlTypeInfo);
        }

        /// <summary>
        /// Deserializes UTF-16 XML text as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="xml">The XML text.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(ReadOnlySpan<char> xml, XmlSerializerContext context)
        {
            return Read<TValue>(xml, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Deserializes UTF-16 XML text as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="xml">The XML text.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(ReadOnlySpan<char> xml, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(xml, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Deserializes UTF-16 XML text as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="xml">The XML text.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static object? Deserialize(ReadOnlySpan<char> xml, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(xml, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Deserializes UTF-8 XML as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The UTF-8 XML.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Xml, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(utf8Xml, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Deserializes UTF-8 XML as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The UTF-8 XML.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(utf8Xml, xmlTypeInfo);
        }

        /// <summary>
        /// Deserializes UTF-8 XML as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The UTF-8 XML.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Xml, XmlSerializerContext context)
        {
            return Read<TValue>(utf8Xml, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Deserializes UTF-8 XML as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 XML.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(ReadOnlySpan<byte> utf8Xml, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(utf8Xml, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Deserializes UTF-8 XML as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 XML.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static object? Deserialize(ReadOnlySpan<byte> utf8Xml, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(utf8Xml, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Deserializes a UTF-8 sequence as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The UTF-8 XML.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(ReadOnlySequence<byte> utf8Xml, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(utf8Xml, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Deserializes a UTF-8 sequence as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The UTF-8 XML.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(ReadOnlySequence<byte> utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(utf8Xml, xmlTypeInfo);
        }

        /// <summary>
        /// Deserializes a UTF-8 sequence as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The UTF-8 XML.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(ReadOnlySequence<byte> utf8Xml, XmlSerializerContext context)
        {
            return Read<TValue>(utf8Xml, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Deserializes a UTF-8 sequence as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 XML.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(ReadOnlySequence<byte> utf8Xml, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(utf8Xml, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Deserializes a UTF-8 sequence as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 XML.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static object? Deserialize(ReadOnlySequence<byte> utf8Xml, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(utf8Xml, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Deserializes the next element of <paramref name="reader"/> as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="reader">The reader, on or before the start tag of the value. It is left on the end of that element.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(ref Utf8XmlReader reader, XmlSerializerOptions? options = null)
        {
            return ReadRoot<TValue>(ref reader, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Deserializes the next element of <paramref name="reader"/> as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="reader">The reader, on or before the start tag of the value. It is left on the end of that element.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(ref Utf8XmlReader reader, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return ReadRoot<TValue>(ref reader, xmlTypeInfo);
        }

        /// <summary>
        /// Deserializes the next element of <paramref name="reader"/> as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="reader">The reader, on or before the start tag of the value. It is left on the end of that element.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(ref Utf8XmlReader reader, XmlSerializerContext context)
        {
            return ReadRoot<TValue>(ref reader, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Deserializes the next element of <paramref name="reader"/> as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="reader">The reader, on or before the start tag of the value. It is left on the end of that element.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(ref Utf8XmlReader reader, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return ReadRoot<object?>(ref reader, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Deserializes the next element of <paramref name="reader"/> as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="reader">The reader, on or before the start tag of the value. It is left on the end of that element.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static object? Deserialize(ref Utf8XmlReader reader, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return ReadRoot<object?>(ref reader, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Deserializes a UTF-8 stream as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The stream, read to its end.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(Stream utf8Xml, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(utf8Xml, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Deserializes a UTF-8 stream as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The stream, read to its end.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(Stream utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(utf8Xml, xmlTypeInfo);
        }

        /// <summary>
        /// Deserializes a UTF-8 stream as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The stream, read to its end.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(Stream utf8Xml, XmlSerializerContext context)
        {
            return Read<TValue>(utf8Xml, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Deserializes a UTF-8 stream as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="utf8Xml">The stream, read to its end.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(Stream utf8Xml, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(utf8Xml, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Deserializes a UTF-8 stream as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="utf8Xml">The stream, read to its end.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static object? Deserialize(Stream utf8Xml, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(utf8Xml, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Deserializes a UTF-8 stream without blocking on it as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The stream, read to its end.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static ValueTask<TValue?> DeserializeAsync<TValue>(Stream utf8Xml, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ReadAsync<TValue>(utf8Xml, GetTypeInfo(typeof(TValue), options), cancellationToken);
        }

        /// <summary>
        /// Deserializes a UTF-8 stream without blocking on it as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The stream, read to its end.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value.</returns>
        public static ValueTask<TValue?> DeserializeAsync<TValue>(Stream utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return ReadAsync<TValue>(utf8Xml, xmlTypeInfo, cancellationToken);
        }

        /// <summary>
        /// Deserializes a UTF-8 stream without blocking on it as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The stream, read to its end.</param>
        /// <param name="context">The serializer context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value.</returns>
        public static ValueTask<TValue?> DeserializeAsync<TValue>(Stream utf8Xml, XmlSerializerContext context, CancellationToken cancellationToken = default)
        {
            return ReadAsync<TValue>(utf8Xml, GetTypeInfo(typeof(TValue), context), cancellationToken);
        }

        /// <summary>
        /// Deserializes a UTF-8 stream without blocking on it as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="utf8Xml">The stream, read to its end.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static ValueTask<object?> DeserializeAsync(Stream utf8Xml, Type returnType, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return ReadAsync<object?>(utf8Xml, GetTypeInfo(returnType, options), cancellationToken);
        }

        /// <summary>
        /// Deserializes a UTF-8 stream without blocking on it as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="utf8Xml">The stream, read to its end.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="context">The serializer context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value.</returns>
        public static ValueTask<object?> DeserializeAsync(Stream utf8Xml, Type returnType, XmlSerializerContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return ReadAsync<object?>(utf8Xml, GetTypeInfo(returnType, context), cancellationToken);
        }

        /// <summary>
        /// Deserializes the root element of <paramref name="document"/> as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="document">The document.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(XmlDocument document, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(document, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Deserializes the root element of <paramref name="document"/> as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="document">The document.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(XmlDocument document, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(document, xmlTypeInfo);
        }

        /// <summary>
        /// Deserializes the root element of <paramref name="document"/> as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="document">The document.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(XmlDocument document, XmlSerializerContext context)
        {
            return Read<TValue>(document, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Deserializes the root element of <paramref name="document"/> as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(XmlDocument document, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(document, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Deserializes the root element of <paramref name="document"/> as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static object? Deserialize(XmlDocument document, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(document, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Deserializes <paramref name="element"/> as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="element">The element.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(XmlElement element, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(element, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Deserializes <paramref name="element"/> as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="element">The element.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(XmlElement element, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(element, xmlTypeInfo);
        }

        /// <summary>
        /// Deserializes <paramref name="element"/> as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="element">The element.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(XmlElement element, XmlSerializerContext context)
        {
            return Read<TValue>(element, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Deserializes <paramref name="element"/> as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(XmlElement element, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(element, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Deserializes <paramref name="element"/> as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static object? Deserialize(XmlElement element, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(element, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Deserializes <paramref name="node"/> as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="node">The node.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(XmlNode node, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(node, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Deserializes <paramref name="node"/> as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="node">The node.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(XmlNode node, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(node, xmlTypeInfo);
        }

        /// <summary>
        /// Deserializes <paramref name="node"/> as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="node">The node.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static TValue? Deserialize<TValue>(XmlNode node, XmlSerializerContext context)
        {
            return Read<TValue>(node, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Deserializes <paramref name="node"/> as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(XmlNode node, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(node, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Deserializes <paramref name="node"/> as <paramref name="returnType"/>.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="returnType">The CLR type.</param>
        /// <param name="context">The serializer context.</param>
        /// <returns>The value.</returns>
        public static object? Deserialize(XmlNode node, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(node, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Deserializes a pipe, read to its end, as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The pipe reader.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static ValueTask<TValue?> DeserializeAsync<TValue>(PipeReader utf8Xml, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ReadAsync<TValue>(utf8Xml, GetTypeInfo(typeof(TValue), options), cancellationToken);
        }

        /// <summary>
        /// Deserializes a pipe, read to its end, as <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The pipe reader.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value.</returns>
        public static ValueTask<TValue?> DeserializeAsync<TValue>(PipeReader utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return ReadAsync<TValue>(utf8Xml, xmlTypeInfo, cancellationToken);
        }

        /// <summary>
        /// Reads each child of the document element in <paramref name="utf8Xml"/> as a <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The stream, read as values are requested.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The values.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream utf8Xml, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return DeserializeAsyncEnumerable<TValue>(utf8Xml, topLevelValues: false, options, cancellationToken);
        }

        /// <summary>
        /// Reads successive values of <typeparamref name="TValue"/> from <paramref name="utf8Xml"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The stream, read as values are requested.</param>
        /// <param name="topLevelValues">When <see langword="true"/>, each document in the stream is a value; when <see langword="false"/>, each child of the document element is a value.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The values.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream utf8Xml, bool topLevelValues, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return DeserializeAsyncEnumerable(utf8Xml, (options ?? XmlSerializerOptions.Default).GetTypeInfo<TValue>(), topLevelValues, cancellationToken);
        }

        /// <summary>
        /// Reads each child of the document element in <paramref name="utf8Xml"/> as a <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The stream, read as values are requested.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The values.</returns>
        public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default)
        {
            return DeserializeAsyncEnumerable(utf8Xml, xmlTypeInfo, topLevelValues: false, cancellationToken);
        }

        /// <summary>
        /// Reads successive values of <typeparamref name="TValue"/> from <paramref name="utf8Xml"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The stream, read as values are requested.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <param name="topLevelValues">When <see langword="true"/>, each document in the stream is a value; when <see langword="false"/>, each child of the document element is a value.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The values.</returns>
        /// <remarks>
        /// Values are returned as the stream delivers them. Each read from the stream is followed by every
        /// value that is now complete, so the buffer holds one read plus at most one unfinished value, not the
        /// whole stream. A malformed document fails when the bad part is reached; values before it have
        /// already been returned.
        /// </remarks>
        public static async IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo, bool topLevelValues, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            var options = xmlTypeInfo.Options.ToReaderOptions();
            options.AllowMultipleRoots = topLevelValues;
            var state = new XmlReaderState(options);
            var scope = default(XmlReaderState);
            var values = new List<TValue?>();
            var buffer = ArrayPool<byte>.Shared.Rent(XmlLimits.Resolve(xmlTypeInfo.Options.DefaultBufferSize, XmlLimits.DefaultBufferSize));
            var length = 0;
            try
            {
                var isFinalBlock = false;
                while (!isFinalBlock)
                {
                    if (length == buffer.Length)
                    {
                        var grown = ArrayPool<byte>.Shared.Rent(length * 2);
                        buffer.AsSpan(0, length).CopyTo(grown);
                        XmlDocument.ReturnBuffer(buffer, length);
                        buffer = grown;
                    }

                    var read = await utf8Xml.ReadAsync(buffer.AsMemory(length), cancellationToken).ConfigureAwait(false);
                    isFinalBlock = read == 0;
                    length += read;
                    var used = 0;
                    ExceptionDispatchInfo? failure = null;
                    try
                    {
                        used = topLevelValues
                            ? ReadAvailableDocuments(buffer.AsSpan(0, length), isFinalBlock, xmlTypeInfo, options, values)
                            : ReadAvailableChildren(buffer.AsSpan(0, length), isFinalBlock, xmlTypeInfo, ref state, ref scope, values);
                    }
                    catch (XmlException exception)
                    {
                        // The values read before the malformed part are returned first.
                        failure = ExceptionDispatchInfo.Capture(exception);
                    }

                    buffer.AsSpan(used, length - used).CopyTo(buffer);
                    length -= used;
                    for (var i = 0; i < values.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        yield return values[i];
                    }

                    values.Clear();
                    failure?.Throw();
                }
            }
            finally
            {
                XmlDocument.ReturnBuffer(buffer, buffer.Length);
            }
        }

        /// <summary>
        /// Reads successive values of <typeparamref name="TValue"/> from <paramref name="utf8Xml"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The pipe reader, read as values are requested.</param>
        /// <param name="topLevelValues">When <see langword="true"/>, each document in the pipe is a value; when <see langword="false"/>, each child of the document element is a value.</param>
        /// <param name="options">The options, or <see langword="null"/> for <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The values.</returns>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(PipeReader utf8Xml, bool topLevelValues, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            return DeserializeAsyncEnumerable<TValue>(utf8Xml.AsStream(leaveOpen: true), topLevelValues, options, cancellationToken);
        }

        /// <summary>
        /// Reads each child of the document element in <paramref name="utf8Xml"/> as a <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The CLR type.</typeparam>
        /// <param name="utf8Xml">The pipe reader, read as values are requested.</param>
        /// <param name="xmlTypeInfo">The type info.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The values.</returns>
        public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(PipeReader utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            return DeserializeAsyncEnumerable(utf8Xml.AsStream(leaveOpen: true), xmlTypeInfo, topLevelValues: false, cancellationToken);
        }

        #endregion

    }

}
