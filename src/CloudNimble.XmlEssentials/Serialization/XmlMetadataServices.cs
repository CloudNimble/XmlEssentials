using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
        /// Adds the entries read for a dictionary member to the dictionary the member already holds.
        /// </summary>
        /// <typeparam name="TKey">
        /// The type of the keys.
        /// </typeparam>
        /// <typeparam name="TValue">
        /// The type of the values.
        /// </typeparam>
        /// <param name="target">The dictionary the member holds.</param>
        /// <param name="entries">The entries that were read, in document order.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="target"/> or <paramref name="entries"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The dictionary is read-only.
        /// </exception>
        /// <remarks>
        /// An entry whose key the dictionary already holds is given the value that was read. This method is for
        /// source-generated converters only, which call it for a member that is populated
        /// (<see cref="XmlObjectCreationHandling.Populate"/>).
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddEntries<TKey, TValue>(object target, IEnumerable<KeyValuePair<TKey, TValue>> entries)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(entries);
            if (target is not IDictionary<TKey, TValue> { IsReadOnly: false } map)
            {
                throw new NotSupportedException($"The dictionary of type '{target.GetType()}' is read-only and cannot be populated.");
            }

            foreach (var entry in entries)
            {
                map[entry.Key] = entry.Value;
            }
        }

        /// <summary>
        /// Adds the items read for a collection member to the collection the member already holds.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the items.
        /// </typeparam>
        /// <param name="target">The collection the member holds.</param>
        /// <param name="items">The items that were read, in document order.</param>
        /// <param name="memberName">The name of the member, for the message of the exception.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="target"/> or <paramref name="items"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The collection cannot take items, as with an array or a read-only collection.
        /// </exception>
        /// <remarks>
        /// This method is for source-generated converters only, which call it for a member that is populated
        /// (<see cref="XmlObjectCreationHandling.Populate"/>).
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddItems<TElement>(object target, List<TElement> items, string memberName)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(items);
            if (!XmlCollectionShape.AddItems(items, target))
            {
                throw new NotSupportedException($"The collection of type '{target.GetType()}' held by the member '{memberName}' is read-only and cannot be populated.");
            }
        }


        /// <summary>
        /// Creates a collection of the declared type from the items that were read.
        /// </summary>
        /// <typeparam name="TCollection">
        /// The declared collection type.
        /// </typeparam>
        /// <typeparam name="TElement">
        /// The type of the items.
        /// </typeparam>
        /// <param name="items">The items that were read, in document order.</param>
        /// <returns>
        /// The collection: <paramref name="items"/> itself when the declared type accepts a list, an array, or a new
        /// collection of the declared type.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="items"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <typeparamref name="TCollection"/> has no constructor and no <c>Add</c> method that the serializer can use.
        /// </exception>
        /// <remarks>
        /// This method is for source-generated converters only. It creates what the reflection resolver creates for
        /// the same declared type.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TCollection CreateCollection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] TCollection, TElement>(List<TElement> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            return (TCollection)XmlCollectionFactory<TCollection, TElement>.Create(items);
        }

        /// <summary>
        /// Creates metadata for an array type whose item type is named at compile time.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the items.
        /// </typeparam>
        /// <param name="options">The serialization options to bind the metadata to.</param>
        /// <returns>
        /// The serialization metadata for an array of <typeparamref name="TElement"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// Generated contexts call this method instead of closing a generic converter through reflection, which Native AOT
        /// cannot do for an item type that is a value type. A converter the caller supplied for the array type is used in
        /// place of the built-in one.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static XmlTypeInfo<TElement[]> CreateArrayInfo<TElement>(XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return CreateCustomInfo<TElement[]>(options)
                ?? CreateValueInfo(options, new XmlCollectionConverter<TElement[]>(
                    new XmlCollectionBinding<TElement>(typeof(TElement[]), wrapped: true, [], options.GetTypeInfo<TElement>(), mixed: false)));
        }

        /// <summary>
        /// Creates metadata for a collection type whose item type is named at compile time.
        /// </summary>
        /// <typeparam name="TCollection">
        /// The collection type.
        /// </typeparam>
        /// <typeparam name="TElement">
        /// The type of the items.
        /// </typeparam>
        /// <param name="options">The serialization options to bind the metadata to.</param>
        /// <returns>
        /// The serialization metadata for type <typeparamref name="TCollection"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// Generated contexts call this method instead of closing a generic converter through reflection, which Native AOT
        /// cannot do for an item type that is a value type. A converter the caller supplied for the collection type is used
        /// in place of the built-in one.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static XmlTypeInfo<TCollection> CreateCollectionInfo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] TCollection, TElement>(XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return CreateCustomInfo<TCollection>(options)
                ?? CreateValueInfo(options, new XmlCollectionConverter<TCollection>(
                    new XmlCollectionBinding<TElement>(typeof(TCollection), wrapped: true, [], options.GetTypeInfo<TElement>(), mixed: false)));
        }

        /// <summary>
        /// Creates metadata for a dictionary type whose key and value types are named at compile time.
        /// </summary>
        /// <typeparam name="TDictionary">
        /// The dictionary type.
        /// </typeparam>
        /// <typeparam name="TKey">
        /// The type of the keys.
        /// </typeparam>
        /// <typeparam name="TValue">
        /// The type of the values.
        /// </typeparam>
        /// <param name="options">The serialization options to bind the metadata to.</param>
        /// <returns>
        /// The serialization metadata for type <typeparamref name="TDictionary"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// Generated contexts call this method instead of closing a generic converter through reflection, which Native AOT
        /// cannot do for a key or value type that is a value type. A converter the caller supplied for the dictionary type
        /// is used in place of the built-in one.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static XmlTypeInfo<TDictionary> CreateDictionaryInfo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDictionary, TKey, TValue>(XmlSerializerOptions options)
            where TDictionary : IEnumerable<KeyValuePair<TKey, TValue>>
            where TKey : notnull
        {
            ArgumentNullException.ThrowIfNull(options);
            return CreateCustomInfo<TDictionary>(options)
                ?? CreateValueInfo(options, new XmlDictionaryConverter<TDictionary, TKey, TValue>(options));
        }

        /// <summary>
        /// Creates metadata for an enumeration type.
        /// </summary>
        /// <typeparam name="T">
        /// The enumeration type.
        /// </typeparam>
        /// <param name="options">The serialization options to bind the metadata to.</param>
        /// <returns>
        /// The serialization metadata for type <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// Generated contexts call this method for an enumeration they list or reach. A converter the caller supplied for the
        /// enumeration, in the options or with <see cref="XmlConverterAttribute"/>, is used in place of the built-in one.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static XmlTypeInfo<T> CreateEnumInfo<T>(XmlSerializerOptions options)
            where T : struct, Enum
        {
            ArgumentNullException.ThrowIfNull(options);
            return CreateCustomInfo<T>(options) ?? CreateValueInfo(options, new XmlEnumConverter<T>());
        }

        /// <summary>
        /// Creates metadata for <see cref="Nullable{T}"/> of a value type that is not one of the built-in scalars.
        /// </summary>
        /// <typeparam name="T">
        /// The value type.
        /// </typeparam>
        /// <param name="options">The serialization options to bind the metadata to.</param>
        /// <returns>
        /// The serialization metadata for the nullable form of type <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The options have no metadata for type <typeparamref name="T"/>.
        /// </exception>
        /// <remarks>
        /// Generated contexts call this method for the nullable form of an enumeration or a struct. The value is read and
        /// written by the converter the options have for <typeparamref name="T"/>, and null is handled around it.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static XmlTypeInfo<T?> CreateNullableInfo<T>(XmlSerializerOptions options)
            where T : struct
        {
            ArgumentNullException.ThrowIfNull(options);
            if (CreateCustomInfo<T?>(options) is { } custom)
            {
                return custom;
            }

            var inner = options.GetTypeInfo<T>().Converter;
            XmlConverter<T?> converter = inner is XmlValueConverter<T> value
                ? new XmlNullableValueConverter<T>(value)
                : new XmlNullableConverter<T>(inner);
            return CreateValueInfo(options, converter);
        }

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
        /// Gets type info for a type whose converter exists already: a built-in scalar or its nullable form, an XML
        /// value, <see cref="object"/>, or a type the caller supplied a converter for.
        /// </summary>
        /// <param name="type">
        /// The type to get type info for.
        /// </param>
        /// <param name="options">
        /// The options that own the type info.
        /// </param>
        /// <returns>
        /// The type info, or <see langword="null"/> when a converter would have to be made for <paramref name="type"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="type"/> or <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// A generated context calls this method for a type it neither lists nor reaches. It answers
        /// <see langword="null"/> for an enumeration, a collection, a dictionary, the nullable form of a value type that is
        /// not a built-in scalar, and a type mapped member by member. Making a converter for one of those takes reflection
        /// or closes a generic type at run time, which Native AOT cannot do. The next resolver in
        /// <see cref="XmlSerializerOptions.TypeInfoResolverChain"/> is then asked, and a type nobody lists is reported as
        /// not supported.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static XmlTypeInfo? GetBuiltInTypeInfo(Type type, XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(options);
            return XmlBuiltInConverters.IsReadyMade(type) || options.HasCustomConverter(type)
                ? DefaultXmlTypeInfoResolver.Instance.GetTypeInfo(type, options)
                : null;
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
        /// Rejects an attribute that no member is mapped to, when the options do not allow unmapped members.
        /// </summary>
        /// <param name="reader">The reader, on the attribute.</param>
        /// <param name="options">The serialization options.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <see cref="XmlSerializerOptions.UnmappedMemberHandling"/> is <see cref="XmlUnmappedMemberHandling.Disallow"/> and
        /// the attribute is not one the serializer itself reads.
        /// </exception>
        /// <remarks>
        /// Namespace declarations, <c>xsi:nil</c>, <c>xsi:type</c>, and the reference attributes are never unmapped. This
        /// method is for source-generated converters only, and applies the rule the reflection resolver applies.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void RejectUnmappedAttribute(ref Utf8XmlReader reader, XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (options.UnmappedMemberHandling is not XmlUnmappedMemberHandling.Disallow
                || reader.PrefixSpan.SequenceEqual("xmlns"u8)
                || (reader.PrefixSpan.IsEmpty && reader.LocalNameEquals("xmlns"u8))
                || reader.NamespaceUriEquals(XmlNamespaces.Refs)
                || ((reader.LocalNameEquals(XmlAttributes.Nil) || reader.LocalNameEquals(XmlAttributes.Type)) && reader.NamespaceUriEquals(XmlNamespaces.Xsi)))
            {
                return;
            }

            throw new XmlException($"Unknown attribute '{System.Text.Encoding.UTF8.GetString(reader.LocalNameSpan)}'.");
        }

        /// <summary>
        /// Rejects an element that no member is mapped to, when the options do not allow unmapped members.
        /// </summary>
        /// <param name="reader">The reader, on the start tag of the element.</param>
        /// <param name="options">The serialization options.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <see cref="XmlSerializerOptions.UnmappedMemberHandling"/> is <see cref="XmlUnmappedMemberHandling.Disallow"/>.
        /// </exception>
        /// <remarks>
        /// This method is for source-generated converters only, and applies the rule the reflection resolver applies.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void RejectUnmappedElement(ref Utf8XmlReader reader, XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (options.UnmappedMemberHandling is XmlUnmappedMemberHandling.Disallow)
            {
                throw new XmlException($"Unknown element '{System.Text.Encoding.UTF8.GetString(reader.LocalNameSpan)}'.");
            }
        }

        /// <summary>
        /// Checks that the options a generated context was given agree with the settings its converters were compiled with.
        /// </summary>
        /// <param name="options">The options of the context.</param>
        /// <param name="elementNamingPolicy">The policy the element names were generated with, or <see langword="null"/> for none.</param>
        /// <param name="attributeNamingPolicy">The policy the attribute names were generated with, or <see langword="null"/> for none.</param>
        /// <param name="defaultPropertyMapping">The property mapping the members were generated with.</param>
        /// <param name="includeFields">Whether the converters were generated with fields included.</param>
        /// <param name="directTypes">The types that the generated converters read and write without a converter.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The options ask for a naming policy, a property mapping, or a choice of members that differs from the one the
        /// converters were compiled with.
        /// -or-
        /// The options supply a converter for one of <paramref name="directTypes"/>.
        /// </exception>
        /// <remarks>
        /// A generated converter has its element and attribute names, and its choice of members, compiled in. With other
        /// options it would write a different document than the reflection resolver writes, so the mismatch is reported
        /// instead. The settings are compiled from <see cref="XmlSourceGenerationOptionsAttribute"/>. The options become
        /// read-only, as they do when they are first used to serialize. This method is for source-generated contexts only.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void VerifyGeneratedOptions(
            XmlSerializerOptions options,
            XmlNamingPolicy? elementNamingPolicy,
            XmlNamingPolicy? attributeNamingPolicy,
            XmlPropertyMapping defaultPropertyMapping,
            bool includeFields,
            params ReadOnlySpan<Type> directTypes)
        {
            ArgumentNullException.ThrowIfNull(options);
            options.MakeReadOnly();
            var elementPolicy = options.ElementNamingPolicy ?? options.PropertyNamingPolicy;
            var attributePolicy = options.AttributeNamingPolicy ?? options.PropertyNamingPolicy;
            ThrowIfCompiledDifferently(nameof(XmlSerializerOptions.ElementNamingPolicy), ReferenceEquals(elementNamingPolicy, elementPolicy), Describe(elementNamingPolicy), Describe(elementPolicy));
            ThrowIfCompiledDifferently(nameof(XmlSerializerOptions.AttributeNamingPolicy), ReferenceEquals(attributeNamingPolicy, attributePolicy), Describe(attributeNamingPolicy), Describe(attributePolicy));
            ThrowIfCompiledDifferently(nameof(XmlSerializerOptions.DefaultPropertyMapping), defaultPropertyMapping == options.DefaultPropertyMapping, defaultPropertyMapping.ToString(), options.DefaultPropertyMapping.ToString());
            ThrowIfCompiledDifferently(nameof(XmlSerializerOptions.IncludeFields), includeFields == options.IncludeFields, includeFields.ToString(), options.IncludeFields.ToString());
            ThrowIfCompiledDifferently(nameof(XmlSerializerOptions.IgnoreReadOnlyProperties), !options.IgnoreReadOnlyProperties, bool.FalseString, bool.TrueString);
            ThrowIfCompiledDifferently(nameof(XmlSerializerOptions.IgnoreReadOnlyFields), !options.IgnoreReadOnlyFields, bool.FalseString, bool.TrueString);
            foreach (var type in directTypes)
            {
                if (options.HasCustomConverter(type))
                {
                    throw new NotSupportedException(
                        $"The options supply a converter for '{type}', and the generated converters read and write that type directly. Use the reflection resolver for these options.");
                }
            }
        }

        /// <summary>
        /// Writes text as CDATA, in as many sections as it takes.
        /// </summary>
        /// <param name="writer">The writer to write to, inside an element.</param>
        /// <param name="text">The text.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="text"/> holds a character that XML does not allow.
        /// </exception>
        /// <remarks>
        /// A CDATA section ends at the first <c>]]&gt;</c>, so text that holds one is cut between the brackets and the angle
        /// bracket and written as adjacent sections, which read back as one value. This method is for source-generated
        /// converters only.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void WriteCData(Utf8XmlWriter writer, ReadOnlySpan<char> text)
        {
            ArgumentNullException.ThrowIfNull(writer);
            XmlConverterHelpers.WriteCDataSections(writer, text);
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

        #region Private Methods

        /// <summary>
        /// Creates metadata from a converter the caller supplied for <typeparamref name="T"/>, if there is one.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="options">The serialization options.</param>
        /// <returns>The metadata, or null when the built-in converter is the one to use.</returns>
        /// <remarks>
        /// A converter in <see cref="XmlSerializerOptions.Converters"/> or named by <see cref="XmlConverterAttribute"/>
        /// comes before a built-in one, for a generated context as for the reflection resolver.
        /// </remarks>
        internal static XmlTypeInfo<T>? CreateCustomInfo<T>(XmlSerializerOptions options)
        {
            return options.HasCustomConverter(typeof(T))
                ? (XmlTypeInfo<T>?)DefaultXmlTypeInfoResolver.Instance.GetTypeInfo(typeof(T), options)
                : null;
        }

        /// <summary>
        /// Names a naming policy for a message.
        /// </summary>
        /// <param name="policy">The policy, or null.</param>
        /// <returns>The name of the policy's type, or <c>none</c>.</returns>
        internal static string Describe(XmlNamingPolicy? policy)
        {
            return policy is null ? "none" : policy.GetType().Name;
        }

        /// <summary>
        /// Reports a setting whose run-time value differs from the one the converters were compiled with.
        /// </summary>
        /// <param name="setting">The name of the option.</param>
        /// <param name="same">Whether the two values are the same.</param>
        /// <param name="compiled">The value the converters were compiled with.</param>
        /// <param name="actual">The value the options have.</param>
        /// <exception cref="NotSupportedException">The two values differ.</exception>
        internal static void ThrowIfCompiledDifferently(string setting, bool same, string compiled, string actual)
        {
            if (!same)
            {
                throw new NotSupportedException(
                    $"The generated converters were compiled with {setting} = {compiled}, and the options have {actual}. Set it with [XmlSourceGenerationOptions] on the context, or use the reflection resolver for these options.");
            }
        }

        #endregion

    }

}
