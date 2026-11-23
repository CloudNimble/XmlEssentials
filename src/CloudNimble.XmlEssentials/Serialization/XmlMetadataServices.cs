using System;
using System.ComponentModel;
using CloudNimble.Text.Xml;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Provides helpers to create and use metadata for XML-serializable types. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// Source-generated converters and contexts call these methods. This class is analogous to <c>JsonMetadataServices</c> in <c>System.Text.Json</c>.
    /// </remarks>
    public static class XmlMetadataServices
    {

        #region Public Methods

        /// <summary>
        /// Creates metadata for a type that is read and written with the specified converter.
        /// </summary>
        /// <typeparam name="T">
        /// The type that the metadata describes.
        /// </typeparam>
        /// <param name="options">The serialization options to bind the metadata to.</param>
        /// <param name="converter">The converter that reads and writes values of type <typeparamref name="T"/>.</param>
        /// <returns>
        /// The serialization metadata for type <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> or <paramref name="converter"/> is <see langword="null"/>.
        /// </exception>
        public static XmlTypeInfo<T> CreateValueInfo<T>(XmlSerializerOptions options, XmlConverter<T> converter)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(converter);
            // A converter for T asked for T always answers, and with type info for T.
            return (XmlTypeInfo<T>)converter.CreateTypeInfo(typeof(T), options)!;
        }

        /// <summary>
        /// Creates the built-in converter for the specified enumeration type.
        /// </summary>
        /// <typeparam name="T">
        /// The enumeration type to convert.
        /// </typeparam>
        /// <returns>
        /// A converter for type <typeparamref name="T"/>.
        /// </returns>
        /// <remarks>
        /// Generated contexts call this method instead of making a generic converter through reflection, which Native AOT cannot do.
        /// </remarks>
        public static XmlConverter<T> CreateEnumConverter<T>()
            where T : struct, Enum
        {
            return new XmlEnumConverter<T>();
        }

        /// <summary>
        /// Reads the element that the reader is positioned on, by using the specified metadata.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the value to read.
        /// </typeparam>
        /// <param name="reader">The reader, positioned on the start tag of the element.</param>
        /// <param name="typeInfo">The metadata for type <typeparamref name="T"/>.</param>
        /// <returns>
        /// The value that the element represents. For an element marked <c>xsi:nil="true"</c>, the default value of
        /// <typeparamref name="T"/> is returned unless the converter handles <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="typeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The element is marked <c>xsi:nil="true"</c>, type <typeparamref name="T"/> cannot be <see langword="null"/>, and the
        /// converter does not handle <see langword="null"/>.
        /// </exception>
        public static T? Read<T>(ref Utf8XmlReader reader, XmlTypeInfo<T> typeInfo)
        {
            ArgumentNullException.ThrowIfNull(typeInfo);
            return typeInfo.ReadElement(ref reader);
        }

        /// <summary>
        /// Reads the attribute that the reader is positioned on, by using the specified metadata.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the value to read.
        /// </typeparam>
        /// <param name="reader">The reader, positioned on an attribute.</param>
        /// <param name="typeInfo">The metadata for type <typeparamref name="T"/>.</param>
        /// <returns>
        /// The value that the attribute represents.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="typeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static T? ReadAttribute<T>(ref Utf8XmlReader reader, XmlTypeInfo<T> typeInfo)
        {
            ArgumentNullException.ThrowIfNull(typeInfo);
            return typeInfo.Converter.ReadAsAttribute(ref reader, typeof(T), typeInfo.Options);
        }

        /// <summary>
        /// Writes the specified value as the content of the current element, by using the specified metadata.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the value to write.
        /// </typeparam>
        /// <param name="writer">The writer to write to, positioned inside the start tag of the element.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="typeInfo">The metadata for type <typeparamref name="T"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/> or <paramref name="typeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// The method calls <see cref="XmlConverter{T}.Write"/> on the converter of <paramref name="typeInfo"/>. It does not write the start tag or the end tag.
        /// </remarks>
        public static void Write<T>(Utf8XmlWriter writer, T value, XmlTypeInfo<T> typeInfo)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(typeInfo);
            typeInfo.Converter.Write(writer, value, typeInfo.Options);
        }

        /// <summary>
        /// Writes the specified value as an attribute with the specified name, by using the specified metadata.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the value to write.
        /// </typeparam>
        /// <param name="writer">The writer to write to, positioned on an open start tag.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="typeInfo">The metadata for type <typeparamref name="T"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/> or <paramref name="typeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static void WriteAttribute<T>(Utf8XmlWriter writer, XmlName name, T value, XmlTypeInfo<T> typeInfo)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(typeInfo);
            typeInfo.Converter.WriteAsAttribute(writer, name, value, typeInfo.Options);
        }

        /// <summary>
        /// Starts a leaf element by writing a start tag that was built ahead of time.
        /// </summary>
        /// <param name="writer">The writer to write to, positioned inside an element.</param>
        /// <param name="openTag">The whole start tag as UTF-8 bytes, in the form <c>&lt;name&gt;</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// The name is longer than <see cref="XmlWriterOptions.MaxNameByteCount"/>.
        /// -or-
        /// The element is deeper than <see cref="XmlWriterOptions.MaxDepth"/>.
        /// -or-
        /// No element is open.
        /// </exception>
        /// <remarks>
        /// This method is for source-generated converters only.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void WriteLeafStart(Utf8XmlWriter writer, ReadOnlySpan<byte> openTag)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteLeafStart(openTag);
        }

        /// <summary>
        /// Ends a leaf element that was started with <see cref="WriteLeafStart"/>, by writing an end tag that was built ahead of time.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="closeTag">The whole end tag as UTF-8 bytes, in the form <c>&lt;/name&gt;</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// No element is open.
        /// </exception>
        /// <remarks>
        /// This method is for source-generated converters only.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void WriteLeafEnd(Utf8XmlWriter writer, ReadOnlySpan<byte> closeTag)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteLeafEnd(closeTag);
        }

        #endregion

    }

}
