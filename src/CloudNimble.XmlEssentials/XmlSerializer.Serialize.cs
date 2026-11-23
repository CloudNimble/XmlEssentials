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
        /// Converts the value of a type specified by a generic type parameter into an XML string, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// An XML string representation of the value.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static string Serialize<TValue>(TValue value, XmlSerializerOptions? options = null)
        {
            return WriteString(value, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Converts the provided value into an XML string, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <returns>
        /// An XML string representation of the value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static string Serialize<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return WriteString(value, xmlTypeInfo);
        }

        /// <summary>
        /// Converts the value of a type specified by a generic type parameter into an XML string, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// An XML string representation of the value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static string Serialize<TValue>(TValue value, XmlSerializerContext context)
        {
            return WriteString(value, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Converts the value of a specified type into an XML string, using the specified options.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// An XML string representation of the value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="inputType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static string Serialize(object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteString(value, GetTypeInfo(inputType, options));
        }

        /// <summary>
        /// Converts the value of a specified type into an XML string, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// An XML string representation of the value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="inputType"/>.
        /// </exception>
        public static string Serialize(object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteString(value, GetTypeInfo(inputType, context));
        }

        /// <summary>
        /// Converts the value of a type specified by a generic type parameter into an XML string, encoded as UTF-8 bytes, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// An XML string representation of the value, encoded as UTF-8 bytes.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, XmlSerializerOptions? options = null)
        {
            return WriteBytes(value, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Converts the provided value into an XML string, encoded as UTF-8 bytes, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <returns>
        /// An XML string representation of the value, encoded as UTF-8 bytes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return WriteBytes(value, xmlTypeInfo);
        }

        /// <summary>
        /// Converts the value of a type specified by a generic type parameter into an XML string, encoded as UTF-8 bytes, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// An XML string representation of the value, encoded as UTF-8 bytes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, XmlSerializerContext context)
        {
            return WriteBytes(value, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Converts the value of a specified type into an XML string, encoded as UTF-8 bytes, using the specified options.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// An XML string representation of the value, encoded as UTF-8 bytes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="inputType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static byte[] SerializeToUtf8Bytes(object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteBytes(value, GetTypeInfo(inputType, options));
        }

        /// <summary>
        /// Converts the value of a specified type into an XML string, encoded as UTF-8 bytes, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// An XML string representation of the value, encoded as UTF-8 bytes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="inputType"/>.
        /// </exception>
        public static byte[] SerializeToUtf8Bytes(object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteBytes(value, GetTypeInfo(inputType, context));
        }

        /// <summary>
        /// Writes the XML representation of a type specified by a generic type parameter to the provided writer as one element, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="writer">The XML writer to write to. It is not flushed.</param>
        /// <param name="value">The value to convert and write.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static void Serialize<TValue>(Utf8XmlWriter writer, TValue value, XmlSerializerOptions? options = null)
        {
            WriteRoot(writer, value, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Writes the XML representation of the provided value to the provided writer as one element, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="writer">The XML writer to write to. It is not flushed.</param>
        /// <param name="value">The value to convert and write.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/> or <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static void Serialize<TValue>(Utf8XmlWriter writer, TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            WriteRoot(writer, value, xmlTypeInfo);
        }

        /// <summary>
        /// Writes the XML representation of a type specified by a generic type parameter to the provided writer as one element, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="writer">The XML writer to write to. It is not flushed.</param>
        /// <param name="value">The value to convert and write.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static void Serialize<TValue>(Utf8XmlWriter writer, TValue value, XmlSerializerContext context)
        {
            WriteRoot(writer, value, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Writes the XML representation of the specified type to the provided writer as one element, using the specified options.
        /// </summary>
        /// <param name="writer">The XML writer to write to. It is not flushed.</param>
        /// <param name="value">The value to convert and write.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/> or <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="inputType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static void Serialize(Utf8XmlWriter writer, object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            WriteRoot(writer, value, GetTypeInfo(inputType, options));
        }

        /// <summary>
        /// Writes the XML representation of the specified type to the provided writer as one element, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="writer">The XML writer to write to. It is not flushed.</param>
        /// <param name="value">The value to convert and write.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/>, <paramref name="inputType"/>, or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="inputType"/>.
        /// </exception>
        public static void Serialize(Utf8XmlWriter writer, object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            WriteRoot(writer, value, GetTypeInfo(inputType, context));
        }

        /// <summary>
        /// Converts the value of a type specified by a generic type parameter to UTF-8 encoded XML text and writes it to a stream, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="utf8Xml">The UTF-8 stream to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Xml"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static void Serialize<TValue>(Stream utf8Xml, TValue value, XmlSerializerOptions? options = null)
        {
            Write(utf8Xml, value, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded XML text and writes it to a stream, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="utf8Xml">The UTF-8 stream to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Xml"/> or <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static void Serialize<TValue>(Stream utf8Xml, TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            Write(utf8Xml, value, xmlTypeInfo);
        }

        /// <summary>
        /// Converts the value of a type specified by a generic type parameter to UTF-8 encoded XML text and writes it to a stream, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="utf8Xml">The UTF-8 stream to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Xml"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static void Serialize<TValue>(Stream utf8Xml, TValue value, XmlSerializerContext context)
        {
            Write(utf8Xml, value, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Converts the value of a specified type to UTF-8 encoded XML text and writes it to a stream, using the specified options.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 stream to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Xml"/> or <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="inputType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static void Serialize(Stream utf8Xml, object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            Write(utf8Xml, value, GetTypeInfo(inputType, options));
        }

        /// <summary>
        /// Converts the value of a specified type to UTF-8 encoded XML text and writes it to a stream, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 stream to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Xml"/>, <paramref name="inputType"/>, or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="inputType"/>.
        /// </exception>
        public static void Serialize(Stream utf8Xml, object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            Write(utf8Xml, value, GetTypeInfo(inputType, context));
        }

        /// <summary>
        /// Converts the value of a type specified by a generic type parameter to UTF-8 encoded XML text and writes it to a buffer writer, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="bufferWriter">The buffer writer to write the UTF-8 encoded XML text to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="bufferWriter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static void Serialize<TValue>(IBufferWriter<byte> bufferWriter, TValue value, XmlSerializerOptions? options = null)
        {
            Write(bufferWriter, value, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded XML text and writes it to a buffer writer, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="bufferWriter">The buffer writer to write the UTF-8 encoded XML text to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="bufferWriter"/> or <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static void Serialize<TValue>(IBufferWriter<byte> bufferWriter, TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            Write(bufferWriter, value, xmlTypeInfo);
        }

        /// <summary>
        /// Converts the value of a type specified by a generic type parameter to UTF-8 encoded XML text and writes it to a buffer writer, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="bufferWriter">The buffer writer to write the UTF-8 encoded XML text to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="bufferWriter"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static void Serialize<TValue>(IBufferWriter<byte> bufferWriter, TValue value, XmlSerializerContext context)
        {
            Write(bufferWriter, value, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Converts the value of a specified type to UTF-8 encoded XML text and writes it to a buffer writer, using the specified options.
        /// </summary>
        /// <param name="bufferWriter">The buffer writer to write the UTF-8 encoded XML text to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="bufferWriter"/> or <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="inputType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static void Serialize(IBufferWriter<byte> bufferWriter, object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            Write(bufferWriter, value, GetTypeInfo(inputType, options));
        }

        /// <summary>
        /// Converts the value of a specified type to UTF-8 encoded XML text and writes it to a buffer writer, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="bufferWriter">The buffer writer to write the UTF-8 encoded XML text to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="bufferWriter"/>, <paramref name="inputType"/>, or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="inputType"/>.
        /// </exception>
        public static void Serialize(IBufferWriter<byte> bufferWriter, object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            Write(bufferWriter, value, GetTypeInfo(inputType, context));
        }

        /// <summary>
        /// Asynchronously converts a value of a type specified by a generic type parameter to UTF-8 encoded XML text and writes it to a stream, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="utf8Xml">The UTF-8 stream to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the write operation.</param>
        /// <returns>
        /// A task that represents the asynchronous write operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Xml"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static Task SerializeAsync<TValue>(Stream utf8Xml, TValue value, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return WriteAsync(utf8Xml, value, GetTypeInfo(typeof(TValue), options), cancellationToken);
        }

        /// <summary>
        /// Asynchronously converts the provided value to UTF-8 encoded XML text and writes it to a stream, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="utf8Xml">The UTF-8 stream to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the write operation.</param>
        /// <returns>
        /// A task that represents the asynchronous write operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// -or-
        /// <paramref name="utf8Xml"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        public static Task SerializeAsync<TValue>(Stream utf8Xml, TValue value, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return WriteAsync(utf8Xml, value, xmlTypeInfo, cancellationToken);
        }

        /// <summary>
        /// Asynchronously converts a value of a type specified by a generic type parameter to UTF-8 encoded XML text and writes it to a stream, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="utf8Xml">The UTF-8 stream to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the write operation.</param>
        /// <returns>
        /// A task that represents the asynchronous write operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is <see langword="null"/>.
        /// -or-
        /// <paramref name="utf8Xml"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static Task SerializeAsync<TValue>(Stream utf8Xml, TValue value, XmlSerializerContext context, CancellationToken cancellationToken = default)
        {
            return WriteAsync(utf8Xml, value, GetTypeInfo(typeof(TValue), context), cancellationToken);
        }

        /// <summary>
        /// Asynchronously converts the value of a specified type to UTF-8 encoded XML text and writes it to a stream, using the specified options.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 stream to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the write operation.</param>
        /// <returns>
        /// A task that represents the asynchronous write operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> is <see langword="null"/>.
        /// -or-
        /// <paramref name="utf8Xml"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="inputType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static Task SerializeAsync(Stream utf8Xml, object? value, Type inputType, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteAsync(utf8Xml, value, GetTypeInfo(inputType, options), cancellationToken);
        }

        /// <summary>
        /// Asynchronously converts the value of a specified type to UTF-8 encoded XML text and writes it to a stream, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 stream to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the write operation.</param>
        /// <returns>
        /// A task that represents the asynchronous write operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// -or-
        /// <paramref name="utf8Xml"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="inputType"/>.
        /// </exception>
        public static Task SerializeAsync(Stream utf8Xml, object? value, Type inputType, XmlSerializerContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteAsync(utf8Xml, value, GetTypeInfo(inputType, context), cancellationToken);
        }

        /// <summary>
        /// Converts the value of a type specified by a generic type parameter into an <see cref="XmlDocument"/>, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// An <see cref="XmlDocument"/> representation of the value. The caller is responsible for disposing it.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static XmlDocument SerializeToDocument<TValue>(TValue value, XmlSerializerOptions? options = null)
        {
            return WriteDocument(value, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Converts the provided value into an <see cref="XmlDocument"/>, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <returns>
        /// An <see cref="XmlDocument"/> representation of the value. The caller is responsible for disposing it.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static XmlDocument SerializeToDocument<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return WriteDocument(value, xmlTypeInfo);
        }

        /// <summary>
        /// Converts the value of a type specified by a generic type parameter into an <see cref="XmlDocument"/>, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// An <see cref="XmlDocument"/> representation of the value. The caller is responsible for disposing it.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static XmlDocument SerializeToDocument<TValue>(TValue value, XmlSerializerContext context)
        {
            return WriteDocument(value, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Converts the value of a specified type into an <see cref="XmlDocument"/>, using the specified options.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// An <see cref="XmlDocument"/> representation of the value. The caller is responsible for disposing it.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="inputType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static XmlDocument SerializeToDocument(object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteDocument(value, GetTypeInfo(inputType, options));
        }

        /// <summary>
        /// Converts the value of a specified type into an <see cref="XmlDocument"/>, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// An <see cref="XmlDocument"/> representation of the value. The caller is responsible for disposing it.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="inputType"/>.
        /// </exception>
        public static XmlDocument SerializeToDocument(object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteDocument(value, GetTypeInfo(inputType, context));
        }

        /// <summary>
        /// Converts the value of a type specified by a generic type parameter into an <see cref="XmlElement"/>, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// An <see cref="XmlElement"/> representation of the value. It is the root element of a new document.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static XmlElement SerializeToElement<TValue>(TValue value, XmlSerializerOptions? options = null)
        {
            return WriteDocument(value, GetTypeInfo(typeof(TValue), options)).RootElement;
        }

        /// <summary>
        /// Converts the provided value into an <see cref="XmlElement"/>, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <returns>
        /// An <see cref="XmlElement"/> representation of the value. It is the root element of a new document.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static XmlElement SerializeToElement<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return WriteDocument(value, xmlTypeInfo).RootElement;
        }

        /// <summary>
        /// Converts the value of a type specified by a generic type parameter into an <see cref="XmlElement"/>, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// An <see cref="XmlElement"/> representation of the value. It is the root element of a new document.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static XmlElement SerializeToElement<TValue>(TValue value, XmlSerializerContext context)
        {
            return WriteDocument(value, GetTypeInfo(typeof(TValue), context)).RootElement;
        }

        /// <summary>
        /// Converts the value of a specified type into an <see cref="XmlElement"/>, using the specified options.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// An <see cref="XmlElement"/> representation of the value. It is the root element of a new document.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="inputType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static XmlElement SerializeToElement(object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteDocument(value, GetTypeInfo(inputType, options)).RootElement;
        }

        /// <summary>
        /// Converts the value of a specified type into an <see cref="XmlElement"/>, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// An <see cref="XmlElement"/> representation of the value. It is the root element of a new document.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="inputType"/>.
        /// </exception>
        public static XmlElement SerializeToElement(object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteDocument(value, GetTypeInfo(inputType, context)).RootElement;
        }

        /// <summary>
        /// Converts the value of a type specified by a generic type parameter into a mutable <see cref="XmlNode"/>, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// An <see cref="XmlNode"/> representation of the value. It is the root node of a new tree.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static XmlNode? SerializeToNode<TValue>(TValue value, XmlSerializerOptions? options = null)
        {
            return WriteNode(value, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Converts the provided value into a mutable <see cref="XmlNode"/>, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <returns>
        /// An <see cref="XmlNode"/> representation of the value. It is the root node of a new tree.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static XmlNode? SerializeToNode<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return WriteNode(value, xmlTypeInfo);
        }

        /// <summary>
        /// Converts the value of a type specified by a generic type parameter into a mutable <see cref="XmlNode"/>, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// An <see cref="XmlNode"/> representation of the value. It is the root node of a new tree.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static XmlNode? SerializeToNode<TValue>(TValue value, XmlSerializerContext context)
        {
            return WriteNode(value, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Converts the value of a specified type into a mutable <see cref="XmlNode"/>, using the specified options.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// An <see cref="XmlNode"/> representation of the value. It is the root node of a new tree.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="inputType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static XmlNode? SerializeToNode(object? value, Type inputType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteNode(value, GetTypeInfo(inputType, options));
        }

        /// <summary>
        /// Converts the value of a specified type into a mutable <see cref="XmlNode"/>, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type to serialize <paramref name="value"/> as.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// An <see cref="XmlNode"/> representation of the value. It is the root node of a new tree.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="inputType"/>.
        /// </exception>
        public static XmlNode? SerializeToNode(object? value, Type inputType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(inputType);
            return WriteNode(value, GetTypeInfo(inputType, context));
        }

        /// <summary>
        /// Asynchronously converts a value of a type specified by a generic type parameter to UTF-8 encoded XML text, writes it to a pipe, and flushes the pipe, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the value to serialize.
        /// </typeparam>
        /// <param name="utf8Xml">The UTF-8 pipe writer to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the flush operation.</param>
        /// <returns>
        /// A task that represents the asynchronous write operation. It completes when the pipe has been flushed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Xml"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled. This exception is stored into the returned task.
        /// </exception>
        /// <remarks>
        /// To write to a pipe without flushing it, pass the <see cref="PipeWriter"/> to the
        /// <see cref="IBufferWriter{T}"/> overload of <c>Serialize</c>.
        /// </remarks>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static async Task SerializeAsync<TValue>(PipeWriter utf8Xml, TValue value, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Xml);
            Write(utf8Xml, value, GetTypeInfo(typeof(TValue), options));
            await utf8Xml.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously converts a sequence of values to UTF-8 encoded XML text and writes them to a stream as the children of one <c>ArrayOf*</c> document element, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the values to serialize.
        /// </typeparam>
        /// <param name="utf8Xml">The UTF-8 stream to write to.</param>
        /// <param name="values">The values to convert, written as they arrive.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the write operation.</param>
        /// <returns>
        /// A task that represents the asynchronous write operation. It completes when the document has been written.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Xml"/> or <paramref name="values"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static Task SerializeAsyncEnumerable<TValue>(Stream utf8Xml, IAsyncEnumerable<TValue> values, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return SerializeAsyncEnumerable(utf8Xml, values, topLevelValues: false, options, cancellationToken);
        }

        /// <summary>
        /// Asynchronously converts a sequence of values to UTF-8 encoded XML text and writes them to a stream, either as separate documents or as the children of one document element, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the values to serialize.
        /// </typeparam>
        /// <param name="utf8Xml">The UTF-8 stream to write to.</param>
        /// <param name="values">The values to convert, written as they arrive.</param>
        /// <param name="topLevelValues"><see langword="true"/> to write each value as its own document; <see langword="false"/> to write the values as the children of one <c>ArrayOf*</c> document element.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the write operation.</param>
        /// <returns>
        /// A task that represents the asynchronous write operation. It completes when every value has been written.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Xml"/> or <paramref name="values"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static Task SerializeAsyncEnumerable<TValue>(Stream utf8Xml, IAsyncEnumerable<TValue> values, bool topLevelValues, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return SerializeAsyncEnumerable(utf8Xml, values, (options ?? XmlSerializerOptions.Default).GetTypeInfo<TValue>(), topLevelValues, cancellationToken);
        }

        /// <summary>
        /// Asynchronously converts a sequence of values to UTF-8 encoded XML text and writes them to a stream, either as separate documents or as the children of one document element, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the values to serialize.
        /// </typeparam>
        /// <param name="utf8Xml">The UTF-8 stream to write to.</param>
        /// <param name="values">The values to convert, written as they arrive.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <param name="topLevelValues"><see langword="true"/> to write each value as its own document; <see langword="false"/> to write the values as the children of one <c>ArrayOf*</c> document element. The default is <see langword="false"/>.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the write operation.</param>
        /// <returns>
        /// A task that represents the asynchronous write operation. It completes when every value has been written.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Xml"/>, <paramref name="values"/>, or <paramref name="xmlTypeInfo"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        /// <remarks>
        /// The <c>ArrayOf*</c> document element is named <c>ArrayOf</c> followed by the local name of the root
        /// element of <typeparamref name="TValue"/>.
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
        /// Asynchronously converts a sequence of values to UTF-8 encoded XML text, writes them to a pipe, either as separate documents or as the children of one document element, and flushes the pipe, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the values to serialize.
        /// </typeparam>
        /// <param name="utf8Xml">The UTF-8 pipe writer to write to.</param>
        /// <param name="values">The values to convert, written as they arrive.</param>
        /// <param name="topLevelValues"><see langword="true"/> to write each value as its own document; <see langword="false"/> to write the values as the children of one <c>ArrayOf*</c> document element.</param>
        /// <param name="options">The options that control serialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the write operation.</param>
        /// <returns>
        /// A task that represents the asynchronous write operation. It completes when the pipe has been flushed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Xml"/> or <paramref name="values"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled. This exception is stored into the returned task.
        /// </exception>
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
