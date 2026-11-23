using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// The converters every <see cref="XmlSerializerOptions"/> falls back to.
    /// </summary>
    internal static class XmlBuiltInConverters
    {

        #region Fields

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
        }.ToFrozenDictionary();

        #endregion

        #region Private Methods

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
                return (XmlConverter)Activator.CreateInstance(typeof(XmlEnumConverter<>).MakeGenericType(type))!;
            }

            if (XmlCollectionConverterFactory.Instance.CanConvert(type))
            {
                return XmlCollectionConverterFactory.Instance.CreateConverter(type, options);
            }

            return XmlObjectBinder.IsObjectType(type)
                ? XmlObjectConverterFactory.Instance.CreateConverter(type, options)
                : null;
        }

        /// <summary>
        /// Returns the default element name for <paramref name="type"/>: the type name, the underlying type name
        /// for <see cref="Nullable{T}"/>, and <c>ByteArray</c> for <c>byte[]</c>.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>The name before the naming policy is applied.</returns>
        internal static string GetXmlTypeName(Type type)
        {
            var root = Nullable.GetUnderlyingType(type) ?? type;
            if (root.IsArray)
            {
                return root == typeof(byte[])
                    ? "ByteArray"
                    : GetXmlTypeName(root.GetElementType()!) + "Array";
            }

            if (!root.IsGenericType)
            {
                return root.Name;
            }

            var name = root.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0)
            {
                name = name[..tick];
            }

            var args = root.GetGenericArguments();
            var length = name.Length + 2;
            foreach (var arg in args)
            {
                length += GetXmlTypeName(arg).Length;
            }

            return string.Create(length, (name, args), static (span, state) =>
            {
                var written = 0;
                state.name.AsSpan().CopyTo(span);
                written += state.name.Length;
                "Of".AsSpan().CopyTo(span[written..]);
                written += 2;
                foreach (var arg in state.args)
                {
                    var argName = GetXmlTypeName(arg);
                    argName.AsSpan().CopyTo(span[written..]);
                    written += argName.Length;
                }
            });
        }

        #endregion

    }

}
