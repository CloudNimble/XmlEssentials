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
    /// Source-generated contexts call these methods. This class is analogous to <c>JsonMetadataServices</c> in <c>System.Text.Json</c>.
    /// </remarks>
    public static class XmlMetadataServices
    {

        #region Public Methods

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
        /// Marks the metadata of an array member with the type of its items.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the items.
        /// </typeparam>
        /// <param name="propertyInfo">The metadata of the member.</param>
        /// <returns>
        /// <paramref name="propertyInfo"/>, which now reads and writes the member as an array of
        /// <typeparamref name="TElement"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="propertyInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// An array has its own method because <see cref="CreateCollectionPropertyInfo{TCollection, TElement}"/> keeps
        /// the constructors and methods of the collection type for the trimmer, and those of <see cref="Array"/> cannot
        /// be kept under Native AOT. An array needs none of them: it is made from the items that were read. This
        /// method is for source-generated contexts only.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static XmlPropertyInfoValues<TElement[]> CreateArrayPropertyInfo<TElement>(XmlPropertyInfoValues<TElement[]> propertyInfo)
        {
            ArgumentNullException.ThrowIfNull(propertyInfo);
            propertyInfo.ElementType = typeof(TElement);
            propertyInfo.CreateCollectionBinding = static (collectionType, wrapped, utf8ItemName, itemTypeInfo, mixed) =>
                new XmlCollectionBinding<TElement>(collectionType, wrapped, utf8ItemName, itemTypeInfo, mixed);
            return propertyInfo;
        }

        /// <summary>
        /// Marks the metadata of a collection member with the type of its items.
        /// </summary>
        /// <typeparam name="TCollection">
        /// The declared type of the member.
        /// </typeparam>
        /// <typeparam name="TElement">
        /// The type of the items.
        /// </typeparam>
        /// <param name="propertyInfo">The metadata of the member.</param>
        /// <returns>
        /// <paramref name="propertyInfo"/>, which now reads and writes the member as a collection of
        /// <typeparamref name="TElement"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="propertyInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// The serializer maps a collection through a type that is generic over the item type. This method closes
        /// it where both types are type arguments, so nothing is closed at run time, which Native AOT cannot do for
        /// a value-type item. This method is for source-generated contexts only.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static XmlPropertyInfoValues<TCollection> CreateCollectionPropertyInfo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] TCollection, TElement>(XmlPropertyInfoValues<TCollection> propertyInfo)
        {
            ArgumentNullException.ThrowIfNull(propertyInfo);
            propertyInfo.ElementType = typeof(TElement);
            propertyInfo.CreateCollectionBinding = static (collectionType, wrapped, utf8ItemName, itemTypeInfo, mixed) =>
                new XmlCollectionBinding<TElement>(collectionType, wrapped, utf8ItemName, itemTypeInfo, mixed);
            return propertyInfo;
        }

        /// <summary>
        /// Marks the metadata of a member whose type is a nullable value type and that names its own converter.
        /// </summary>
        /// <typeparam name="T">
        /// The underlying value type, which the converter converts.
        /// </typeparam>
        /// <param name="propertyInfo">The metadata of the member.</param>
        /// <returns>
        /// <paramref name="propertyInfo"/>, which now wraps the member's converter so that it serves the nullable type.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="propertyInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// A converter for <c>T</c> named by <see cref="XmlConverterAttribute"/> on a member of type <c>T?</c> converts
        /// the value, and <see langword="null"/> is handled around it. This method closes that wrapper where
        /// <typeparamref name="T"/> is a type argument. This method is for source-generated contexts only.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static XmlPropertyInfoValues<T?> CreateNullablePropertyInfo<T>(XmlPropertyInfoValues<T?> propertyInfo)
            where T : struct
        {
            ArgumentNullException.ThrowIfNull(propertyInfo);
            propertyInfo.WrapNullable = static converter => new XmlNullableConverter<T>((XmlConverter<T>)converter);
            return propertyInfo;
        }

        /// <summary>
        /// Creates metadata for an object type from the metadata of its constructor and members.
        /// </summary>
        /// <typeparam name="T">
        /// The object type.
        /// </typeparam>
        /// <param name="options">
        /// The options the metadata is for.
        /// </param>
        /// <param name="objectInfo">
        /// The metadata of the constructor and the members of the type.
        /// </param>
        /// <returns>
        /// Type info that reads and writes <typeparamref name="T"/> with the rules the serializer applies to a type
        /// it maps through reflection.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> or <paramref name="objectInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="objectInfo"/> has a creator that takes constructor arguments and no metadata for the parameters.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The names, the member order, the ignore conditions, the constructor mapping, polymorphism, reference
        /// handling and every other option are applied here, by the code that applies them for the reflection
        /// resolver. A converter the caller supplied for <typeparamref name="T"/> is used in place of the metadata.
        /// The members of <typeparamref name="T"/> are never reflected over, so the result is usable in a
        /// trimmed application and under Native AOT.
        /// </para>
        /// <para>
        /// This method is for source-generated contexts only. It is analogous to
        /// <c>JsonMetadataServices.CreateObjectInfo&lt;T&gt;</c> in <c>System.Text.Json</c>.
        /// </para>
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static XmlTypeInfo<T> CreateObjectInfo<T>(XmlSerializerOptions options, XmlObjectInfoValues<T> objectInfo)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(objectInfo);
            if (objectInfo.ObjectWithParameterizedConstructorCreator is not null && objectInfo.ConstructorParameterMetadataInitializer is null)
            {
                throw new ArgumentException($"{nameof(XmlObjectInfoValues<T>.ObjectWithParameterizedConstructorCreator)} needs {nameof(XmlObjectInfoValues<T>.ConstructorParameterMetadataInitializer)}: the serializer has to know the parameters it fills.", nameof(objectInfo));
            }

            // A converter the caller supplied for the type, in the options or with an attribute on the type, reads
            // and writes it. The metadata is for a type nobody claimed.
            if (CreateCustomInfo<T>(options) is { } custom)
            {
                return custom;
            }

            // An abstract type with no derived types, declared or inferred under these options, has nothing that
            // could be created for it, and writing a value as it would drop the members of the value's own type.
            // The reflection resolver refuses such a type, and with the same message.
            if (!XmlObjectBinder.IsObjectType(typeof(T), options))
            {
                throw new NotSupportedException($"The type '{typeof(T)}' is not supported.");
            }

            var converter = new XmlObjectConverter<T>(options, bindingOptions => XmlObjectBinder.Bind(objectInfo, bindingOptions), objectInfo.SerializeHandler);
            return (XmlTypeInfo<T>)converter.CreateTypeInfo(typeof(T), options)!;
        }

        /// <summary>
        /// Marks the metadata of a member whose type is a value type.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of the member.
        /// </typeparam>
        /// <param name="propertyInfo">The metadata of the member.</param>
        /// <returns>
        /// <paramref name="propertyInfo"/>, which can now hold a value for a constructor without boxing it again
        /// for each object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="propertyInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// When a type is created through a constructor, the serializer holds each value until the element ends. A
        /// value type is held in one box that is written again for the next object. That box is generic over the
        /// member's type, and this method closes it where the type is a type argument. This method is for
        /// source-generated contexts only.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static XmlPropertyInfoValues<TValue> CreateValuePropertyInfo<TValue>(XmlPropertyInfoValues<TValue> propertyInfo)
            where TValue : struct
        {
            ArgumentNullException.ThrowIfNull(propertyInfo);
            propertyInfo.CreateHeldBox = static () => new XmlStructHeldBox<TValue>();
            return propertyInfo;
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
        /// This method is for the write fast path of a source-generated context only.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void WriteLeafStart(Utf8XmlWriter writer, ReadOnlySpan<byte> openTag)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteLeafStart(openTag);
        }

        /// <summary>
        /// Returns whether the write fast path a context generated can be used with the given options.
        /// </summary>
        /// <param name="options">The options the type info is for.</param>
        /// <param name="elementNamingPolicy">The policy the element names were generated with, or <see langword="null"/> for none.</param>
        /// <param name="attributeNamingPolicy">The policy the attribute names were generated with, or <see langword="null"/> for none.</param>
        /// <param name="defaultPropertyMapping">The property mapping the members were generated with.</param>
        /// <param name="includeFields">Whether the fast path was generated with fields included.</param>
        /// <param name="directTypes">The types the fast path writes without a converter, the type itself first.</param>
        /// <returns>
        /// <see langword="true"/> when the fast path writes what the serializer would write from the metadata with
        /// these options; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// The fast path has its element and attribute names and its choice of members compiled in, from
        /// <see cref="XmlSourceGenerationOptionsAttribute"/>. It is not used when the options ask for another naming
        /// policy, property mapping or choice of members, when they handle references, or when they supply a converter
        /// for a type it writes directly. The serializer then writes from the metadata, which follows every option.
        /// This method is for source-generated contexts only.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool CanUseSerializeHandler(
            XmlSerializerOptions options,
            XmlNamingPolicy? elementNamingPolicy,
            XmlNamingPolicy? attributeNamingPolicy,
            XmlPropertyMapping defaultPropertyMapping,
            bool includeFields,
            params ReadOnlySpan<Type> directTypes)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (options.ReferenceHandler is not null
                || !ReferenceEquals(elementNamingPolicy, options.ElementNamingPolicy ?? options.PropertyNamingPolicy)
                || !ReferenceEquals(attributeNamingPolicy, options.AttributeNamingPolicy ?? options.PropertyNamingPolicy)
                || defaultPropertyMapping != options.DefaultPropertyMapping
                || includeFields != options.IncludeFields
                || options.IgnoreReadOnlyProperties
                || options.IgnoreReadOnlyFields)
            {
                return false;
            }

            foreach (var type in directTypes)
            {
                if (options.HasCustomConverter(type))
                {
                    return false;
                }
            }

            return true;
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
        /// bracket and written as adjacent sections, which read back as one value. This method is for the write fast path of a
        /// source-generated context only.
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
        /// This method is for the write fast path of a source-generated context only.
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

        #endregion

    }

}
