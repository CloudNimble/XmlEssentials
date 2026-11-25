using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using CloudNimble.Text.Xml.Nodes;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// The converters every <see cref="XmlSerializerOptions"/> falls back to.
    /// </summary>
    internal static class XmlBuiltInConverters
    {

        #region Fields

        /// <summary>
        /// The converters for values that are not one text value and not mapped member by member: values that
        /// are XML already, and <see cref="object"/>.
        /// </summary>
        /// <remarks>
        /// They are kept apart from the scalar converters because a scalar can be an attribute or text, and
        /// these cannot. They are looked up before the collection converters because an
        /// <see cref="XmlElementNode"/> enumerates its children.
        /// </remarks>
        private static readonly FrozenDictionary<Type, XmlConverter> s_contentConverters = new Dictionary<Type, XmlConverter>
        {
            [typeof(object)] = XmlRuntimeObjectConverter.Instance,
            [typeof(XmlDocument)] = XmlDocumentFragmentConverter.Instance,
            [typeof(XmlElement)] = XmlElementFragmentConverter.Instance,
            [typeof(XmlElementNode)] = XmlNodeFragmentConverter<XmlElementNode>.Instance,
            [typeof(XmlNode)] = XmlNodeFragmentConverter<XmlNode>.Instance,
        }.ToFrozenDictionary();

        private static readonly FrozenDictionary<Type, XmlConverter> s_converters = new Dictionary<Type, XmlConverter>
        {
            [typeof(bool)] = XmlBooleanConverter.Instance,
            [typeof(byte)] = XmlByteConverter.Instance,
            [typeof(sbyte)] = XmlSByteConverter.Instance,
            [typeof(short)] = XmlInt16Converter.Instance,
            [typeof(ushort)] = XmlUInt16Converter.Instance,
            [typeof(int)] = XmlInt32Converter.Instance,
            [typeof(uint)] = XmlUInt32Converter.Instance,
            [typeof(long)] = XmlInt64Converter.Instance,
            [typeof(ulong)] = XmlUInt64Converter.Instance,
            [typeof(float)] = XmlSingleConverter.Instance,
            [typeof(double)] = XmlDoubleConverter.Instance,
            [typeof(decimal)] = XmlDecimalConverter.Instance,
            [typeof(string)] = XmlStringConverter.Instance,
            [typeof(char)] = XmlCharConverter.Instance,
            [typeof(Guid)] = XmlGuidConverter.Instance,
            [typeof(DateTime)] = XmlDateTimeConverter.Instance,
            [typeof(DateTimeOffset)] = XmlDateTimeOffsetConverter.Instance,
            [typeof(DateOnly)] = XmlDateOnlyConverter.Instance,
            [typeof(TimeOnly)] = XmlTimeOnlyConverter.Instance,
            [typeof(TimeSpan)] = XmlTimeSpanConverter.Instance,
            [typeof(byte[])] = XmlByteArrayConverter.Instance,
            [typeof(Uri)] = XmlUriConverter.Instance,
            [typeof(Version)] = XmlVersionConverter.Instance,
            [typeof(Int128)] = XmlInt128Converter.Instance,
            [typeof(UInt128)] = XmlUInt128Converter.Instance,
            [typeof(Half)] = XmlHalfConverter.Instance,
            [typeof(BigInteger)] = XmlBigIntegerConverter.Instance,
            [typeof(Memory<byte>)] = XmlMemoryConverter.Instance,
            [typeof(ReadOnlyMemory<byte>)] = XmlReadOnlyMemoryConverter.Instance,
            [typeof(bool?)] = new XmlNullableValueConverter<bool>(XmlBooleanConverter.Instance),
            [typeof(byte?)] = new XmlNullableValueConverter<byte>(XmlByteConverter.Instance),
            [typeof(sbyte?)] = new XmlNullableValueConverter<sbyte>(XmlSByteConverter.Instance),
            [typeof(short?)] = new XmlNullableValueConverter<short>(XmlInt16Converter.Instance),
            [typeof(ushort?)] = new XmlNullableValueConverter<ushort>(XmlUInt16Converter.Instance),
            [typeof(int?)] = new XmlNullableValueConverter<int>(XmlInt32Converter.Instance),
            [typeof(uint?)] = new XmlNullableValueConverter<uint>(XmlUInt32Converter.Instance),
            [typeof(long?)] = new XmlNullableValueConverter<long>(XmlInt64Converter.Instance),
            [typeof(ulong?)] = new XmlNullableValueConverter<ulong>(XmlUInt64Converter.Instance),
            [typeof(float?)] = new XmlNullableValueConverter<float>(XmlSingleConverter.Instance),
            [typeof(double?)] = new XmlNullableValueConverter<double>(XmlDoubleConverter.Instance),
            [typeof(decimal?)] = new XmlNullableValueConverter<decimal>(XmlDecimalConverter.Instance),
            [typeof(char?)] = new XmlNullableValueConverter<char>(XmlCharConverter.Instance),
            [typeof(Guid?)] = new XmlNullableValueConverter<Guid>(XmlGuidConverter.Instance),
            [typeof(DateTime?)] = new XmlNullableValueConverter<DateTime>(XmlDateTimeConverter.Instance),
            [typeof(DateTimeOffset?)] = new XmlNullableValueConverter<DateTimeOffset>(XmlDateTimeOffsetConverter.Instance),
            [typeof(DateOnly?)] = new XmlNullableValueConverter<DateOnly>(XmlDateOnlyConverter.Instance),
            [typeof(TimeOnly?)] = new XmlNullableValueConverter<TimeOnly>(XmlTimeOnlyConverter.Instance),
            [typeof(TimeSpan?)] = new XmlNullableValueConverter<TimeSpan>(XmlTimeSpanConverter.Instance),
            [typeof(Int128?)] = new XmlNullableValueConverter<Int128>(XmlInt128Converter.Instance),
            [typeof(UInt128?)] = new XmlNullableValueConverter<UInt128>(XmlUInt128Converter.Instance),
            [typeof(Half?)] = new XmlNullableValueConverter<Half>(XmlHalfConverter.Instance),
            [typeof(BigInteger?)] = new XmlNullableValueConverter<BigInteger>(XmlBigIntegerConverter.Instance),
            [typeof(Memory<byte>?)] = new XmlNullableValueConverter<Memory<byte>>(XmlMemoryConverter.Instance),
            [typeof(ReadOnlyMemory<byte>?)] = new XmlNullableValueConverter<ReadOnlyMemory<byte>>(XmlReadOnlyMemoryConverter.Instance),
        }.ToFrozenDictionary();

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the ready-made converter for <paramref name="type"/>: one that exists already, so that no
        /// generic type is closed and no member is looked up to make it.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="options">The options, which say whether the caller replaced the converter of a <see cref="Nullable{T}"/>'s underlying type.</param>
        /// <returns>The converter, or <see langword="null"/> when the type has no ready-made converter.</returns>
        /// <remarks>
        /// A <see cref="Nullable{T}"/> has one when its underlying type has one and the caller has not replaced
        /// it. Generated metadata uses this method, so it must stay free of reflection; <see cref="GetReflected"/>
        /// makes every other built-in converter.
        /// </remarks>
        internal static XmlConverter? GetReadyMade(Type type, XmlSerializerOptions options)
        {
            if (Nullable.GetUnderlyingType(type) is { } underlying)
            {
                return s_converters.TryGetValue(type, out var nullable)
                    && ReferenceEquals(options.GetDeclaredConverterOrNull(underlying), s_converters[underlying])
                    ? nullable
                    : null;
            }

            if (s_converters.TryGetValue(type, out var converter))
            {
                return converter;
            }

            return s_contentConverters.TryGetValue(type, out var content) ? content : null;
        }

        /// <summary>
        /// Makes the built-in converter for a type that has no ready-made one, by closing a generic converter over
        /// the type at run time.
        /// </summary>
        /// <param name="type">The CLR type, for which <see cref="GetReadyMade"/> returned <see langword="null"/>.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The converter, or <see langword="null"/> when the type has no built-in converter.</returns>
        /// <remarks>
        /// These are an enumeration, a <see cref="Nullable{T}"/> whose underlying type is an enumeration, has a
        /// caller-supplied converter or is not in the table, a collection, a dictionary, and an object. Only the
        /// reflection resolver comes here. A generated context names each of these types where it is compiled.
        /// </remarks>
        [RequiresUnreferencedCode(XmlSerializer.RefMsg), RequiresDynamicCode(XmlSerializer.DynMsg)]
        internal static XmlConverter? GetReflected(Type type, XmlSerializerOptions options)
        {
            if (Nullable.GetUnderlyingType(type) is { } underlying)
            {
                if (options.GetConverterOrNull(underlying) is not { } inner)
                {
                    return null;
                }

                return inner.IsValueConverter
                    ? (XmlConverter)Activator.CreateInstance(typeof(XmlNullableValueConverter<>).MakeGenericType(underlying), inner)!
                    : (XmlConverter)Activator.CreateInstance(typeof(XmlNullableConverter<>).MakeGenericType(underlying), inner)!;
            }

            if (type.IsEnum)
            {
                try
                {
                    return (XmlConverter)Activator.CreateInstance(typeof(XmlEnumConverter<>).MakeGenericType(type))!;
                }
                catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is InvalidOperationException reason)
                {
                    // The converter refuses a member name it could not read back. The caller gets the same
                    // kind of exception and message a generated context gives, and not the wrapper that
                    // reflection puts around it.
                    throw new InvalidOperationException(reason.Message, ex);
                }
            }

            if (XmlCollectionConverterFactory.Instance.CanConvert(type))
            {
                return XmlCollectionConverterFactory.Instance.CreateConverter(type, options);
            }

            return XmlObjectBinder.IsObjectType(type, options)
                ? XmlObjectConverterFactory.Instance.CreateConverter(type, options)
                : null;
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> is a built-in scalar (or nullable scalar or enum) that can be
        /// an XML attribute.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns><see langword="true"/> for primitives, enums, strings, and the other built-in value converters.</returns>
        internal static bool IsScalar(Type type)
        {
            var root = Nullable.GetUnderlyingType(type) ?? type;
            return s_converters.ContainsKey(root) || root.IsEnum;
        }

        #endregion

    }

}
