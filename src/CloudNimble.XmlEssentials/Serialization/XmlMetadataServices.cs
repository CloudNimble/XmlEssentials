using System;
using System.ComponentModel;
using CloudNimble.Text.Xml;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Helpers used by source-generated converters and contexts. Analogous to <c>JsonMetadataServices</c>.
    /// </summary>
    public static class XmlMetadataServices
    {

        #region Public Methods

        /// <summary>
        /// Creates type info that reads and writes <typeparamref name="T"/> with <paramref name="converter"/>.
        /// </summary>
        /// <typeparam name="T">The CLR type.</typeparam>
        /// <param name="options">The serializer options.</param>
        /// <param name="converter">The converter.</param>
        /// <returns>The type info.</returns>
        public static XmlTypeInfo<T> CreateValueInfo<T>(XmlSerializerOptions options, XmlConverter<T> converter)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(converter);
            // A converter for T asked for T always answers, and with type info for T.
            return (XmlTypeInfo<T>)converter.CreateTypeInfo(typeof(T), options)!;
        }

        /// <summary>
        /// Returns the built-in converter for enum <typeparamref name="T"/>. Generated contexts use this instead of
        /// making a generic converter through reflection, which Native AOT cannot do.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <returns>The converter.</returns>
        public static XmlConverter<T> CreateEnumConverter<T>()
            where T : struct, Enum
        {
            return new XmlEnumConverter<T>();
        }

        /// <summary>
        /// Reads the element <paramref name="reader"/> is on using <paramref name="typeInfo"/>.
        /// </summary>
        /// <typeparam name="T">The CLR type.</typeparam>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The value.</returns>
        public static T? Read<T>(ref Utf8XmlReader reader, XmlTypeInfo<T> typeInfo)
        {
            ArgumentNullException.ThrowIfNull(typeInfo);
            return typeInfo.ReadElement(ref reader);
        }

        /// <summary>
        /// Reads the current attribute using <paramref name="typeInfo"/>.
        /// </summary>
        /// <typeparam name="T">The CLR type.</typeparam>
        /// <param name="reader">The reader, on an attribute.</param>
        /// <param name="typeInfo">The type info.</param>
        /// <returns>The value.</returns>
        public static T? ReadAttribute<T>(ref Utf8XmlReader reader, XmlTypeInfo<T> typeInfo)
        {
            ArgumentNullException.ThrowIfNull(typeInfo);
            return typeInfo.Converter.ReadAsAttribute(ref reader, typeof(T), typeInfo.Options);
        }

        /// <summary>
        /// Writes <paramref name="value"/> as element or attribute content using <paramref name="typeInfo"/>.
        /// </summary>
        /// <typeparam name="T">The CLR type.</typeparam>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="typeInfo">The type info.</param>
        public static void Write<T>(Utf8XmlWriter writer, T value, XmlTypeInfo<T> typeInfo)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(typeInfo);
            typeInfo.Converter.Write(writer, value, typeInfo.Options);
        }

        /// <summary>
        /// Writes <paramref name="value"/> as an attribute named <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="T">The CLR type.</typeparam>
        /// <param name="writer">The writer, on an open start tag.</param>
        /// <param name="name">The attribute name.</param>
        /// <param name="value">The value.</param>
        /// <param name="typeInfo">The type info.</param>
        public static void WriteAttribute<T>(Utf8XmlWriter writer, XmlName name, T value, XmlTypeInfo<T> typeInfo)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(typeInfo);
            typeInfo.Converter.WriteAsAttribute(writer, name, value, typeInfo.Options);
        }

        /// <summary>
        /// Starts a leaf element from a prebuilt start tag. For source-generated converters only.
        /// </summary>
        /// <param name="writer">The writer, inside an element.</param>
        /// <param name="openTag">The whole start tag, <c>&lt;name&gt;</c>.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void WriteLeafStart(Utf8XmlWriter writer, ReadOnlySpan<byte> openTag)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteLeafStart(openTag);
        }

        /// <summary>
        /// Ends a leaf element started with <see cref="WriteLeafStart"/>. For source-generated converters only.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="closeTag">The whole end tag, <c>&lt;/name&gt;</c>.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void WriteLeafEnd(Utf8XmlWriter writer, ReadOnlySpan<byte> closeTag)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteLeafEnd(closeTag);
        }

        #endregion

    }

}
