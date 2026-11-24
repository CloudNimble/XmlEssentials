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
        /// Returns whether the built-in converter for <paramref name="type"/> exists already, so that getting it
        /// closes no generic type at run time.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>
        /// <see langword="true"/> for the built-in scalars and their nullable forms, the XML values, and
        /// <see cref="object"/>. <see langword="false"/> for enumerations, collections, dictionaries, the
        /// nullable form of any other value type, and types mapped member by member: each of those needs a
        /// converter made for it.
        /// </returns>
        internal static bool IsReadyMade(Type type)
        {
            return s_converters.ContainsKey(type) || s_contentConverters.ContainsKey(type);
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

        /// <summary>
        /// Returns the built-in converter for <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="options">The options a <see cref="Nullable{T}"/> uses to find the converter for its underlying type.</param>
        /// <returns>The converter, or <see langword="null"/> when the type has no built-in converter.</returns>
        /// <remarks>
        /// A <see cref="Nullable{T}"/> wraps whatever converter the options have for <c>T</c>. When that is the
        /// built-in one, the wrapper comes from the table, which is built without reflection. Generic converters
        /// are closed at run time only for enums and for a <see cref="Nullable{T}"/> whose <c>T</c> is an enum,
        /// has a caller-supplied converter, or is not in the table.
        /// </remarks>
        [UnconditionalSuppressMessage("Trimming", "IL2071", Justification = "Both generic converters have one public constructor, and the type argument is a type the caller already holds.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Built-in types and their nullable forms come from the table. Closing a generic converter at run time is needed only for enums and caller-defined value types, which Native AOT callers supply through a context.")]
        internal static XmlConverter? Get(Type type, XmlSerializerOptions options)
        {
            if (Nullable.GetUnderlyingType(type) is { } underlying)
            {
                if (options.GetConverterOrNull(underlying) is not { } inner)
                {
                    return null;
                }

                if (s_converters.TryGetValue(type, out var nullable) && ReferenceEquals(inner, s_converters[underlying]))
                {
                    return nullable;
                }

                return inner.IsValueConverter
                    ? (XmlConverter)Activator.CreateInstance(typeof(XmlNullableValueConverter<>).MakeGenericType(underlying), inner)!
                    : (XmlConverter)Activator.CreateInstance(typeof(XmlNullableConverter<>).MakeGenericType(underlying), inner)!;
            }

            if (s_converters.TryGetValue(type, out var converter))
            {
                return converter;
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

            if (s_contentConverters.TryGetValue(type, out var content))
            {
                return content;
            }

            if (XmlCollectionConverterFactory.Instance.CanConvert(type))
            {
                return XmlCollectionConverterFactory.Instance.CreateConverter(type, options);
            }

            return XmlObjectBinder.IsObjectType(type, options)
                ? XmlObjectConverterFactory.Instance.CreateConverter(type, options)
                : null;
        }

        #endregion

    }

}
