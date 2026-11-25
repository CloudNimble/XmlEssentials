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
        /// Parses the text representing a single XML document into an instance of the type specified by a generic type parameter, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The XML text to parse.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(string xml, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(xml, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Parses the text representing a single XML document into a <typeparamref name="TValue"/>, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The XML text to parse.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> or <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(string xml, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(xml, xmlTypeInfo);
        }

        /// <summary>
        /// Parses the text representing a single XML document into an instance of the type specified by a generic type parameter, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The XML text to parse.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(string xml, XmlSerializerContext context)
        {
            return Read<TValue>(xml, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Parses the text representing a single XML document into an instance of a specified type, using the specified options.
        /// </summary>
        /// <param name="xml">The XML text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> or <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(string xml, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(xml, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Parses the text representing a single XML document into an instance of a specified type, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="xml">The XML text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/>, <paramref name="returnType"/>, or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="returnType"/>.
        /// </exception>
        public static object? Deserialize(string xml, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(xml, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Parses the UTF-16 text span representing a single XML document into an instance of the type specified by a generic type parameter, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The UTF-16 XML text to parse.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(ReadOnlySpan<char> xml, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(xml, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Parses the UTF-16 text span representing a single XML document into a <typeparamref name="TValue"/>, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The UTF-16 XML text to parse.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(ReadOnlySpan<char> xml, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(xml, xmlTypeInfo);
        }

        /// <summary>
        /// Parses the UTF-16 text span representing a single XML document into an instance of the type specified by a generic type parameter, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The UTF-16 XML text to parse.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(ReadOnlySpan<char> xml, XmlSerializerContext context)
        {
            return Read<TValue>(xml, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Parses the UTF-16 text span representing a single XML document into an instance of a specified type, using the specified options.
        /// </summary>
        /// <param name="xml">The UTF-16 XML text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(ReadOnlySpan<char> xml, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(xml, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Parses the UTF-16 text span representing a single XML document into an instance of a specified type, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="xml">The UTF-16 XML text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="returnType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="returnType"/>.
        /// </exception>
        public static object? Deserialize(ReadOnlySpan<char> xml, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(xml, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Parses the UTF-8 encoded text representing a single XML document into an instance of the type specified by a generic type parameter, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The UTF-8 encoded XML text to parse.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> xml, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(xml, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Parses the UTF-8 encoded text representing a single XML document into a <typeparamref name="TValue"/>, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The UTF-8 encoded XML text to parse.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> xml, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(xml, xmlTypeInfo);
        }

        /// <summary>
        /// Parses the UTF-8 encoded text representing a single XML document into an instance of the type specified by a generic type parameter, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The UTF-8 encoded XML text to parse.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> xml, XmlSerializerContext context)
        {
            return Read<TValue>(xml, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Parses the UTF-8 encoded text representing a single XML document into an instance of a specified type, using the specified options.
        /// </summary>
        /// <param name="xml">The UTF-8 encoded XML text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(ReadOnlySpan<byte> xml, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(xml, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Parses the UTF-8 encoded text representing a single XML document into an instance of a specified type, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="xml">The UTF-8 encoded XML text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="returnType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="returnType"/>.
        /// </exception>
        public static object? Deserialize(ReadOnlySpan<byte> xml, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(xml, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Parses the UTF-8 encoded byte sequence representing a single XML document into an instance of the type specified by a generic type parameter, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The sequence of UTF-8 encoded XML text to parse.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(ReadOnlySequence<byte> xml, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(xml, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Parses the UTF-8 encoded byte sequence representing a single XML document into a <typeparamref name="TValue"/>, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The sequence of UTF-8 encoded XML text to parse.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(ReadOnlySequence<byte> xml, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(xml, xmlTypeInfo);
        }

        /// <summary>
        /// Parses the UTF-8 encoded byte sequence representing a single XML document into an instance of the type specified by a generic type parameter, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The sequence of UTF-8 encoded XML text to parse.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(ReadOnlySequence<byte> xml, XmlSerializerContext context)
        {
            return Read<TValue>(xml, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Parses the UTF-8 encoded byte sequence representing a single XML document into an instance of a specified type, using the specified options.
        /// </summary>
        /// <param name="xml">The sequence of UTF-8 encoded XML text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(ReadOnlySequence<byte> xml, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(xml, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Parses the UTF-8 encoded byte sequence representing a single XML document into an instance of a specified type, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="xml">The sequence of UTF-8 encoded XML text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="returnType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not well formed.
        /// -or-
        /// <paramref name="xml"/> is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="returnType"/>.
        /// </exception>
        public static object? Deserialize(ReadOnlySequence<byte> xml, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(xml, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Reads one XML element from the provided reader into an instance of the type specified by a generic type parameter, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="reader">The reader to read from, passed by reference. It must be positioned on or before the start tag of the value, and it is left on the end of that element.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(ref Utf8XmlReader reader, XmlSerializerOptions? options = null)
        {
            return ReadRoot<TValue>(ref reader, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Reads one XML element from the provided reader into a <typeparamref name="TValue"/>, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="reader">The reader to read from, passed by reference. It must be positioned on or before the start tag of the value, and it is left on the end of that element.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(ref Utf8XmlReader reader, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return ReadRoot<TValue>(ref reader, xmlTypeInfo);
        }

        /// <summary>
        /// Reads one XML element from the provided reader into an instance of the type specified by a generic type parameter, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="reader">The reader to read from, passed by reference. It must be positioned on or before the start tag of the value, and it is left on the end of that element.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(ref Utf8XmlReader reader, XmlSerializerContext context)
        {
            return ReadRoot<TValue>(ref reader, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Reads one XML element from the provided reader into an instance of a specified type, using the specified options.
        /// </summary>
        /// <param name="reader">The reader to read from, passed by reference. It must be positioned on or before the start tag of the value, and it is left on the end of that element.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(ref Utf8XmlReader reader, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return ReadRoot<object?>(ref reader, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Reads one XML element from the provided reader into an instance of a specified type, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="reader">The reader to read from, passed by reference. It must be positioned on or before the start tag of the value, and it is left on the end of that element.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="returnType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="returnType"/>.
        /// </exception>
        public static object? Deserialize(ref Utf8XmlReader reader, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return ReadRoot<object?>(ref reader, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Reads the UTF-8 encoded text representing a single XML document from a stream into an instance of the type specified by a generic type parameter, using the specified options. The stream is read to completion.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The stream of UTF-8 encoded XML text to parse.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <typeparamref name="TValue"/>.
        /// -or-
        /// The stream holds more bytes than <see cref="XmlSerializerOptions.MaxDocumentByteCount"/> allows.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(Stream xml, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(xml, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Reads the UTF-8 encoded text representing a single XML document from a stream into a <typeparamref name="TValue"/>, using the specified type metadata. The stream is read to completion.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The stream of UTF-8 encoded XML text to parse.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> or <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <typeparamref name="TValue"/>.
        /// -or-
        /// The stream holds more bytes than <see cref="XmlSerializerOptions.MaxDocumentByteCount"/> allows.
        /// </exception>
        public static TValue? Deserialize<TValue>(Stream xml, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(xml, xmlTypeInfo);
        }

        /// <summary>
        /// Reads the UTF-8 encoded text representing a single XML document from a stream into an instance of the type specified by a generic type parameter, using type metadata from the specified serializer context. The stream is read to completion.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The stream of UTF-8 encoded XML text to parse.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <typeparamref name="TValue"/>.
        /// -or-
        /// The stream holds more bytes than <see cref="XmlSerializerOptions.MaxDocumentByteCount"/> allows.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(Stream xml, XmlSerializerContext context)
        {
            return Read<TValue>(xml, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Reads the UTF-8 encoded text representing a single XML document from a stream into an instance of a specified type, using the specified options. The stream is read to completion.
        /// </summary>
        /// <param name="xml">The stream of UTF-8 encoded XML text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> or <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <paramref name="returnType"/>.
        /// -or-
        /// The stream holds more bytes than <see cref="XmlSerializerOptions.MaxDocumentByteCount"/> allows.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(Stream xml, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(xml, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Reads the UTF-8 encoded text representing a single XML document from a stream into an instance of a specified type, using type metadata from the specified serializer context. The stream is read to completion.
        /// </summary>
        /// <param name="xml">The stream of UTF-8 encoded XML text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/>, <paramref name="returnType"/>, or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <paramref name="returnType"/>.
        /// -or-
        /// The stream holds more bytes than <see cref="XmlSerializerOptions.MaxDocumentByteCount"/> allows.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="returnType"/>.
        /// </exception>
        public static object? Deserialize(Stream xml, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(xml, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Asynchronously reads the UTF-8 encoded text representing a single XML document from a stream into an instance of the type specified by a generic type parameter, using the specified options. The stream is read to completion.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The stream of UTF-8 encoded XML text to parse.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
        /// <returns>
        /// A task object that, when awaited, produces a <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <typeparamref name="TValue"/>.
        /// -or-
        /// The stream holds more bytes than <see cref="XmlSerializerOptions.MaxDocumentByteCount"/> allows.
        /// This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static ValueTask<TValue?> DeserializeAsync<TValue>(Stream xml, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ReadAsync<TValue>(xml, GetTypeInfo(typeof(TValue), options), cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the UTF-8 encoded text representing a single XML document from a stream into a <typeparamref name="TValue"/>, using the specified type metadata. The stream is read to completion.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The stream of UTF-8 encoded XML text to parse.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
        /// <returns>
        /// A task object that, when awaited, produces a <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// -or-
        /// <paramref name="xml"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <typeparamref name="TValue"/>.
        /// -or-
        /// The stream holds more bytes than <see cref="XmlSerializerOptions.MaxDocumentByteCount"/> allows.
        /// This exception is stored into the returned task.
        /// </exception>
        public static ValueTask<TValue?> DeserializeAsync<TValue>(Stream xml, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return ReadAsync<TValue>(xml, xmlTypeInfo, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the UTF-8 encoded text representing a single XML document from a stream into an instance of the type specified by a generic type parameter, using type metadata from the specified serializer context. The stream is read to completion.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The stream of UTF-8 encoded XML text to parse.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
        /// <returns>
        /// A task object that, when awaited, produces a <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is <see langword="null"/>.
        /// -or-
        /// <paramref name="xml"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <typeparamref name="TValue"/>.
        /// -or-
        /// The stream holds more bytes than <see cref="XmlSerializerOptions.MaxDocumentByteCount"/> allows.
        /// This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static ValueTask<TValue?> DeserializeAsync<TValue>(Stream xml, XmlSerializerContext context, CancellationToken cancellationToken = default)
        {
            return ReadAsync<TValue>(xml, GetTypeInfo(typeof(TValue), context), cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the UTF-8 encoded text representing a single XML document from a stream into an instance of a specified type, using the specified options. The stream is read to completion.
        /// </summary>
        /// <param name="xml">The stream of UTF-8 encoded XML text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
        /// <returns>
        /// A task object that, when awaited, produces a <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="returnType"/> is <see langword="null"/>.
        /// -or-
        /// <paramref name="xml"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <paramref name="returnType"/>.
        /// -or-
        /// The stream holds more bytes than <see cref="XmlSerializerOptions.MaxDocumentByteCount"/> allows.
        /// This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static ValueTask<object?> DeserializeAsync(Stream xml, Type returnType, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return ReadAsync<object?>(xml, GetTypeInfo(returnType, options), cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the UTF-8 encoded text representing a single XML document from a stream into an instance of a specified type, using type metadata from the specified serializer context. The stream is read to completion.
        /// </summary>
        /// <param name="xml">The stream of UTF-8 encoded XML text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
        /// <returns>
        /// A task object that, when awaited, produces a <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="returnType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// -or-
        /// <paramref name="xml"/> is <see langword="null"/>. This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <paramref name="returnType"/>.
        /// -or-
        /// The stream holds more bytes than <see cref="XmlSerializerOptions.MaxDocumentByteCount"/> allows.
        /// This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="returnType"/>.
        /// </exception>
        public static ValueTask<object?> DeserializeAsync(Stream xml, Type returnType, XmlSerializerContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return ReadAsync<object?>(xml, GetTypeInfo(returnType, context), cancellationToken);
        }

        /// <summary>
        /// Converts the root element of an <see cref="XmlDocument"/> into an instance of the type specified by a generic type parameter, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="document">The <see cref="XmlDocument"/> to convert.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="document"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The root element of <paramref name="document"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(XmlDocument document, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(document, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Converts the root element of an <see cref="XmlDocument"/> into a <typeparamref name="TValue"/>, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="document">The <see cref="XmlDocument"/> to convert.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="document"/> or <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The root element of <paramref name="document"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(XmlDocument document, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(document, xmlTypeInfo);
        }

        /// <summary>
        /// Converts the root element of an <see cref="XmlDocument"/> into an instance of the type specified by a generic type parameter, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="document">The <see cref="XmlDocument"/> to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="document"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The root element of <paramref name="document"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(XmlDocument document, XmlSerializerContext context)
        {
            return Read<TValue>(document, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Converts the root element of an <see cref="XmlDocument"/> into an instance of a specified type, using the specified options.
        /// </summary>
        /// <param name="document">The <see cref="XmlDocument"/> to convert.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="document"/> or <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The root element of <paramref name="document"/> is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(XmlDocument document, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(document, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Converts the root element of an <see cref="XmlDocument"/> into an instance of a specified type, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="document">The <see cref="XmlDocument"/> to convert.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="document"/>, <paramref name="returnType"/>, or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The root element of <paramref name="document"/> is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="returnType"/>.
        /// </exception>
        public static object? Deserialize(XmlDocument document, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(document, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Converts the <see cref="XmlElement"/> representing a single XML value into an instance of the type specified by a generic type parameter, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="element">The <see cref="XmlElement"/> to convert.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="XmlException">
        /// <paramref name="element"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(XmlElement element, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(element, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Converts the <see cref="XmlElement"/> representing a single XML value into a <typeparamref name="TValue"/>, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="element">The <see cref="XmlElement"/> to convert.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="element"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(XmlElement element, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(element, xmlTypeInfo);
        }

        /// <summary>
        /// Converts the <see cref="XmlElement"/> representing a single XML value into an instance of the type specified by a generic type parameter, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="element">The <see cref="XmlElement"/> to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="element"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(XmlElement element, XmlSerializerContext context)
        {
            return Read<TValue>(element, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Converts the <see cref="XmlElement"/> representing a single XML value into an instance of a specified type, using the specified options.
        /// </summary>
        /// <param name="element">The <see cref="XmlElement"/> to convert.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="element"/> is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(XmlElement element, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(element, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Converts the <see cref="XmlElement"/> representing a single XML value into an instance of a specified type, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="element">The <see cref="XmlElement"/> to convert.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="returnType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="element"/> is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="returnType"/>.
        /// </exception>
        public static object? Deserialize(XmlElement element, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(element, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Converts the <see cref="XmlNode"/> representing a single XML value into an instance of the type specified by a generic type parameter, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="node">The <see cref="XmlNode"/> to convert.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="node"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="node"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static TValue? Deserialize<TValue>(XmlNode node, XmlSerializerOptions? options = null)
        {
            return Read<TValue>(node, GetTypeInfo(typeof(TValue), options));
        }

        /// <summary>
        /// Converts the <see cref="XmlNode"/> representing a single XML value into a <typeparamref name="TValue"/>, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="node">The <see cref="XmlNode"/> to convert.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="node"/> or <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="node"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(XmlNode node, XmlTypeInfo<TValue> xmlTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return Read<TValue>(node, xmlTypeInfo);
        }

        /// <summary>
        /// Converts the <see cref="XmlNode"/> representing a single XML value into an instance of the type specified by a generic type parameter, using type metadata from the specified serializer context.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="node">The <see cref="XmlNode"/> to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="node"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="node"/> is not valid for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <typeparamref name="TValue"/>.
        /// </exception>
        public static TValue? Deserialize<TValue>(XmlNode node, XmlSerializerContext context)
        {
            return Read<TValue>(node, GetTypeInfo(typeof(TValue), context));
        }

        /// <summary>
        /// Converts the <see cref="XmlNode"/> representing a single XML value into an instance of a specified type, using the specified options.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> to convert.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="node"/> or <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="node"/> is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static object? Deserialize(XmlNode node, Type returnType, XmlSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(node, GetTypeInfo(returnType, options));
        }

        /// <summary>
        /// Converts the <see cref="XmlNode"/> representing a single XML value into an instance of a specified type, using type metadata from the specified serializer context.
        /// </summary>
        /// <param name="node">The <see cref="XmlNode"/> to convert.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <returns>
        /// A <paramref name="returnType"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="node"/>, <paramref name="returnType"/>, or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="node"/> is not valid for <paramref name="returnType"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="context"/> has no type metadata for <paramref name="returnType"/>.
        /// </exception>
        public static object? Deserialize(XmlNode node, Type returnType, XmlSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            return Read<object?>(node, GetTypeInfo(returnType, context));
        }

        /// <summary>
        /// Asynchronously reads the UTF-8 encoded text representing a single XML document from a pipe into an instance of the type specified by a generic type parameter, using the specified options. The pipe is read to completion.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The pipe reader of UTF-8 encoded XML text to parse.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
        /// <returns>
        /// A task object that, when awaited, produces a <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <typeparamref name="TValue"/>.
        /// -or-
        /// The pipe holds more bytes than <see cref="XmlSerializerOptions.MaxDocumentByteCount"/> allows.
        /// This exception is stored into the returned task.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static ValueTask<TValue?> DeserializeAsync<TValue>(PipeReader xml, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return ReadAsync<TValue>(xml, GetTypeInfo(typeof(TValue), options), cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the UTF-8 encoded text representing a single XML document from a pipe into a <typeparamref name="TValue"/>, using the specified type metadata. The pipe is read to completion.
        /// </summary>
        /// <typeparam name="TValue">
        /// The target type of the XML value.
        /// </typeparam>
        /// <param name="xml">The pipe reader of UTF-8 encoded XML text to parse.</param>
        /// <param name="xmlTypeInfo">The metadata about the type to convert.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
        /// <returns>
        /// A task object that, when awaited, produces a <typeparamref name="TValue"/> representation of the XML value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> or <paramref name="xmlTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// The XML is not valid for <typeparamref name="TValue"/>.
        /// -or-
        /// The pipe holds more bytes than <see cref="XmlSerializerOptions.MaxDocumentByteCount"/> allows.
        /// This exception is stored into the returned task.
        /// </exception>
        public static ValueTask<TValue?> DeserializeAsync<TValue>(PipeReader xml, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            return ReadAsync<TValue>(xml, xmlTypeInfo, cancellationToken);
        }

        /// <summary>
        /// Wraps the UTF-8 encoded text in a stream into an <see cref="IAsyncEnumerable{T}"/> that deserializes each child of the document element in a streaming manner, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The element type to deserialize asynchronously.
        /// </typeparam>
        /// <param name="xml">The stream of UTF-8 encoded XML text to parse. It is read as values are requested.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> representation of the children of the document element.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> is <see langword="null"/>. This exception is thrown when enumeration starts.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// A value is not valid for <typeparamref name="TValue"/>.
        /// This exception is thrown during enumeration, when the invalid part is reached.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream xml, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return DeserializeAsyncEnumerable<TValue>(xml, topLevelValues: false, options, cancellationToken);
        }

        /// <summary>
        /// Wraps the UTF-8 encoded text in a stream into an <see cref="IAsyncEnumerable{T}"/> that deserializes either a sequence of documents or the children of one document element in a streaming manner, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The element type to deserialize asynchronously.
        /// </typeparam>
        /// <param name="xml">The stream of UTF-8 encoded XML text to parse. It is read as values are requested.</param>
        /// <param name="topLevelValues"><see langword="true"/> to read each document in the stream as a value; <see langword="false"/> to read each child of the document element as a value.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> representation of the values in the stream.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> is <see langword="null"/>. This exception is thrown when enumeration starts.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// A value is not valid for <typeparamref name="TValue"/>.
        /// This exception is thrown during enumeration, when the invalid part is reached.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream xml, bool topLevelValues, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return DeserializeAsyncEnumerable(xml, (XmlTypeInfo<TValue>)GetTypeInfo(typeof(TValue), options), topLevelValues, cancellationToken);
        }

        /// <summary>
        /// Wraps the UTF-8 encoded text in a stream into an <see cref="IAsyncEnumerable{T}"/> that deserializes each child of the document element in a streaming manner, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The element type to deserialize asynchronously.
        /// </typeparam>
        /// <param name="xml">The stream of UTF-8 encoded XML text to parse. It is read as values are requested.</param>
        /// <param name="xmlTypeInfo">The metadata about the element type to convert.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> representation of the children of the document element.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> or <paramref name="xmlTypeInfo"/> is <see langword="null"/>. This exception is thrown when enumeration starts.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// A value is not valid for <typeparamref name="TValue"/>.
        /// This exception is thrown during enumeration, when the invalid part is reached.
        /// </exception>
        public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream xml, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default)
        {
            return DeserializeAsyncEnumerable(xml, xmlTypeInfo, topLevelValues: false, cancellationToken);
        }

        /// <summary>
        /// Wraps the UTF-8 encoded text in a stream into an <see cref="IAsyncEnumerable{T}"/> that deserializes either a sequence of documents or the children of one document element in a streaming manner, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The element type to deserialize asynchronously.
        /// </typeparam>
        /// <param name="xml">The stream of UTF-8 encoded XML text to parse. It is read as values are requested.</param>
        /// <param name="xmlTypeInfo">The metadata about the element type to convert.</param>
        /// <param name="topLevelValues"><see langword="true"/> to read each document in the stream as a value; <see langword="false"/> to read each child of the document element as a value.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> representation of the values in the stream.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> or <paramref name="xmlTypeInfo"/> is <see langword="null"/>. This exception is thrown when enumeration starts.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// A value is not valid for <typeparamref name="TValue"/>.
        /// This exception is thrown during enumeration, when the invalid part is reached.
        /// </exception>
        /// <remarks>
        /// Values are returned as the stream delivers them. Each read from the stream is followed by every
        /// value that is now complete, so the buffer holds one read plus at most one unfinished value, not the
        /// whole stream. A malformed document fails when the bad part is reached; values before it have
        /// already been returned.
        /// </remarks>
        public static async IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream xml, XmlTypeInfo<TValue> xmlTypeInfo, bool topLevelValues, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(xml);
            ArgumentNullException.ThrowIfNull(xmlTypeInfo);
            var options = xmlTypeInfo.Options.ToReaderOptions();
            options.AllowMultipleRoots = topLevelValues;
            var state = new XmlReaderState(options);
            var scope = default(XmlReaderState);
            var values = new List<TValue?>();

            // The values of one document share one set of references. Each top-level value is a document of
            // its own, and gets its own.
            var handler = xmlTypeInfo.Options.ReferenceHandler;
            var shared = topLevelValues ? null : handler?.CreateResolver();

            // An object that can be read a piece at a time keeps what it has read between blocks, as
            // System.Text.Json keeps it. One that is tracked for references, or that cannot stop part way, is
            // read once all of it has arrived.
            var stack = !topLevelValues && handler is null && xmlTypeInfo.ResumableBinding is { } resumable && resumable.IsResumable() ? new XmlReadStack() : null;
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

                    var read = await xml.ReadAsync(buffer.AsMemory(length), cancellationToken).ConfigureAwait(false);
                    isFinalBlock = read == 0;
                    length += read;
                    var used = 0;
                    ExceptionDispatchInfo? failure = null;
                    try
                    {
                        used = topLevelValues
                            ? ReadAvailableDocuments(buffer.AsSpan(0, length), isFinalBlock, xmlTypeInfo, options, handler, values)
                            : ReadAvailableChildren(buffer.AsSpan(0, length), isFinalBlock, xmlTypeInfo, ref state, ref scope, shared, stack, values);
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
        /// Wraps the UTF-8 encoded text in a pipe into an <see cref="IAsyncEnumerable{T}"/> that deserializes either a sequence of documents or the children of one document element in a streaming manner, using the specified options.
        /// </summary>
        /// <typeparam name="TValue">
        /// The element type to deserialize asynchronously.
        /// </typeparam>
        /// <param name="xml">The pipe reader of UTF-8 encoded XML text to parse. It is read as values are requested.</param>
        /// <param name="topLevelValues"><see langword="true"/> to read each document in the pipe as a value; <see langword="false"/> to read each child of the document element as a value.</param>
        /// <param name="options">The options that control deserialization, or <see langword="null"/> to use <see cref="XmlSerializerOptions.Default"/>.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> representation of the values in the pipe.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// A value is not valid for <typeparamref name="TValue"/>.
        /// This exception is thrown during enumeration, when the invalid part is reached.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No type metadata is available for <typeparamref name="TValue"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No <see cref="XmlSerializerOptions.TypeInfoResolver"/> is set and reflection-based serialization is disabled.
        /// </exception>
        [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
        public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(PipeReader xml, bool topLevelValues, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(xml);
            return DeserializeAsyncEnumerable<TValue>(xml.AsStream(leaveOpen: true), topLevelValues, options, cancellationToken);
        }

        /// <summary>
        /// Wraps the UTF-8 encoded text in a pipe into an <see cref="IAsyncEnumerable{T}"/> that deserializes each child of the document element in a streaming manner, using the specified type metadata.
        /// </summary>
        /// <typeparam name="TValue">
        /// The element type to deserialize asynchronously.
        /// </typeparam>
        /// <param name="xml">The pipe reader of UTF-8 encoded XML text to parse. It is read as values are requested.</param>
        /// <param name="xmlTypeInfo">The metadata about the element type to convert.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> representation of the children of the document element.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> is <see langword="null"/>.
        /// -or-
        /// <paramref name="xmlTypeInfo"/> is <see langword="null"/>. This exception is thrown when enumeration starts.
        /// </exception>
        /// <exception cref="XmlException">
        /// The XML is not well formed.
        /// -or-
        /// A value is not valid for <typeparamref name="TValue"/>.
        /// This exception is thrown during enumeration, when the invalid part is reached.
        /// </exception>
        public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(PipeReader xml, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(xml);
            return DeserializeAsyncEnumerable(xml.AsStream(leaveOpen: true), xmlTypeInfo, topLevelValues: false, cancellationToken);
        }

        #endregion

    }

}
