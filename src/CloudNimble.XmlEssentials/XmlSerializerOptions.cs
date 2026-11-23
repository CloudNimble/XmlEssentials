using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Options for <see cref="XmlSerializer"/>. Analogous to <c>JsonSerializerOptions</c>.
    /// </summary>
    /// <remarks>
    /// An instance becomes read-only the first time it is used to serialize or deserialize, or when
    /// <see cref="MakeReadOnly"/> is called. Create one instance and reuse it: the converters and type info it
    /// resolves are cached on the instance.
    /// </remarks>
    public sealed class XmlSerializerOptions
    {

        #region Fields

        private readonly ConcurrentDictionary<Type, XmlConverter?> _converters = new();
        private XmlTypeInfo? _lastTypeInfo;
        private int _resolveDepth;
        private readonly object _typeInfoLock = new();
        private XmlTypeInfoResolverChain? _typeInfoResolverChain;
        private IXmlTypeInfoResolver? _typeInfoResolver;
        private readonly ConcurrentDictionary<Type, XmlTypeInfo?> _typeInfos = new();

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets whether duplicate properties are allowed on deserialize.
        /// </summary>
        public bool AllowDuplicateProperties
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        } = true;

        /// <summary>
        /// Gets or sets whether metadata properties may arrive out of order.
        /// </summary>
        public bool AllowOutOfOrderMetadataProperties
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the naming policy for attributes, falling back to <see cref="PropertyNamingPolicy"/>.
        /// </summary>
        public XmlNamingPolicy? AttributeNamingPolicy
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets the converter list. Looked up before <see cref="XmlConverterAttribute"/> on the type and before the built-in converters.
        /// </summary>
        public IList<XmlConverter> Converters { get; }

        /// <summary>
        /// Gets a read-only instance with the general defaults.
        /// </summary>
        public static XmlSerializerOptions Default { get; } = CreateReadOnly(XmlSerializerDefaults.General);

        /// <summary>
        /// Gets or sets the size of the first buffer rented when serializing. 0 means 16384.
        /// </summary>
        public int DefaultBufferSize
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets when members are ignored.
        /// </summary>
        public XmlIgnoreCondition DefaultIgnoreCondition
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the default namespace of the document element.
        /// </summary>
        public string? DefaultNamespace
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets how unannotated properties are mapped.
        /// </summary>
        /// <remarks>
        /// The default is <see cref="XmlPropertyMapping.Element"/>: public properties become child elements,
        /// matching JSON properties in System.Text.Json. Set <see cref="XmlPropertyMapping.Attribute"/> to write
        /// unannotated simple members as XML attributes without annotating each one. Nested objects and
        /// collections stay elements. <c>[XmlElement]</c>, <c>[XmlAttribute]</c>, and <c>[XmlText]</c> override
        /// this per member.
        /// </remarks>
        public XmlPropertyMapping DefaultPropertyMapping
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the naming policy for dictionary keys.
        /// </summary>
        public XmlNamingPolicy? DictionaryKeyPolicy
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the naming policy for elements, falling back to <see cref="PropertyNamingPolicy"/>.
        /// </summary>
        public XmlNamingPolicy? ElementNamingPolicy
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to emit a UTF-8 BOM.
        /// </summary>
        public bool EmitByteOrderMark
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets how enums are written.
        /// </summary>
        public XmlEnumHandling EnumHandling
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets whether read-only fields are ignored.
        /// </summary>
        public bool IgnoreReadOnlyFields
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets whether read-only properties are ignored.
        /// </summary>
        public bool IgnoreReadOnlyProperties
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets whether public fields are included.
        /// </summary>
        public bool IncludeFields
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the indent character.
        /// </summary>
        public char IndentCharacter
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        } = ' ';

        /// <summary>
        /// Gets or sets the indent size. 0 means 2.
        /// </summary>
        public int IndentSize
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets whether closed generic polymorphism is inferred.
        /// </summary>
        public bool InferClosedTypePolymorphism
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets whether this instance can no longer be changed.
        /// </summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// Gets or sets the maximum attributes on one element. 0 means 1000.
        /// </summary>
        public int MaxAttributeCount
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum comment payload. 0 means 1,048,576.
        /// </summary>
        public int MaxCommentByteCount
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum element depth. 0 means 64.
        /// </summary>
        public int MaxDepth
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum document size. 0 stays unbounded.
        /// </summary>
        public int MaxDocumentByteCount
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum name length. 0 means 1024.
        /// </summary>
        public int MaxNameByteCount
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum text node. 0 means 16,777,216.
        /// </summary>
        public int MaxTextByteCount
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the newline used when indented.
        /// </summary>
        public string NewLine
        {
            get;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                VerifyMutable();
                field = value;
            }
        } = Environment.NewLine;

        /// <summary>
        /// Gets or sets how null values are written.
        /// </summary>
        public XmlNilHandling NilHandling
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets whether <c>INF</c>, <c>-INF</c> and <c>NaN</c> are read and written.
        /// </summary>
        public XmlNumberHandling NumberHandling
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        } = XmlNumberHandling.AllowNamedFloatingPointLiterals;

        /// <summary>
        /// Gets or sets whether the XML declaration is omitted.
        /// </summary>
        public bool OmitXmlDeclaration
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets how objects are created when populating an existing instance.
        /// </summary>
        public XmlObjectCreationHandling PreferredObjectCreationHandling
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets whether property names are compared case-insensitively.
        /// </summary>
        public bool PropertyNameCaseInsensitive
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the fallback naming policy for properties, elements, and attributes.
        /// </summary>
        public XmlNamingPolicy? PropertyNamingPolicy
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets how comments are handled when deserializing.
        /// </summary>
        public XmlCommentHandling ReadCommentHandling
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        } = XmlCommentHandling.Skip;

        /// <summary>
        /// Gets or sets the reference handler.
        /// </summary>
        public XmlReferenceHandler? ReferenceHandler
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets whether nullable annotations are respected.
        /// </summary>
        public bool RespectNullableAnnotations
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets whether missing required constructor parameters throw.
        /// Default is <see langword="true"/>.
        /// </summary>
        public bool RespectRequiredConstructorParameters
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        } = true;

        /// <summary>
        /// Gets a read-only instance with the strict defaults.
        /// </summary>
        public static XmlSerializerOptions Strict { get; } = CreateReadOnly(XmlSerializerDefaults.Strict);

        /// <summary>
        /// Gets or sets the resolver that supplies <see cref="XmlTypeInfo"/>.
        /// </summary>
        /// <remarks>
        /// When none has been set, this is <see cref="DefaultXmlTypeInfoResolver.Instance"/> if
        /// <see cref="XmlSerializer.IsReflectionEnabledByDefault"/> is <see langword="true"/> and
        /// <see langword="null"/> otherwise. <see cref="TypeInfoResolverChain"/> is a view of this resolver.
        /// </remarks>
        public IXmlTypeInfoResolver? TypeInfoResolver
        {
            get => _typeInfoResolver ?? (XmlSerializer.IsReflectionEnabledByDefault ? DefaultXmlTypeInfoResolver.Instance : null);
            set
            {
                VerifyMutable();
                _typeInfoResolver = value;
                _typeInfoResolverChain?.Reset(value);
            }
        }

        /// <summary>
        /// Gets the resolvers that make up <see cref="TypeInfoResolver"/>, in the order they are asked.
        /// </summary>
        /// <remarks>
        /// The list is empty until a resolver has been set. Changing the list makes it the
        /// <see cref="TypeInfoResolver"/>; setting <see cref="TypeInfoResolver"/> replaces the list's content.
        /// </remarks>
        public IList<IXmlTypeInfoResolver> TypeInfoResolverChain => _typeInfoResolverChain ??= new XmlTypeInfoResolverChain(this, _typeInfoResolver);

        /// <summary>
        /// Gets or sets how <see cref="object"/> members without a converter are materialized.
        /// </summary>
        public XmlUnknownTypeHandling UnknownTypeHandling
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets how unmapped members are treated.
        /// </summary>
        public XmlUnmappedMemberHandling UnmappedMemberHandling
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets a read-only instance with the web defaults (camelCase, no declaration).
        /// </summary>
        public static XmlSerializerOptions Web { get; } = CreateReadOnly(XmlSerializerDefaults.Web);

        /// <summary>
        /// Gets or sets whitespace handling forwarded to the reader on deserialize.
        /// </summary>
        public XmlWhitespaceHandling WhitespaceHandling
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets whether output is indented.
        /// </summary>
        public bool WriteIndented
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the XML version written in the declaration.
        /// </summary>
        public XmlVersion XmlVersion
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates options with the general defaults.
        /// </summary>
        public XmlSerializerOptions()
        {
            Converters = new XmlConverterList(this, []);
        }

        /// <summary>
        /// Creates options from a named default set.
        /// </summary>
        /// <param name="defaults">The default set.</param>
        public XmlSerializerOptions(XmlSerializerDefaults defaults)
            : this()
        {
            if (defaults is XmlSerializerDefaults.Web)
            {
                PropertyNamingPolicy = XmlNamingPolicy.CamelCase;
                OmitXmlDeclaration = true;
            }
            else if (defaults is XmlSerializerDefaults.Strict)
            {
                AllowDuplicateProperties = false;
                ReadCommentHandling = XmlCommentHandling.Disallow;
                UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow;
            }
        }

        /// <summary>
        /// Copies <paramref name="options"/>. The copy is not read-only and shares no cached state with the source.
        /// </summary>
        /// <param name="options">The source.</param>
        public XmlSerializerOptions(XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            AllowDuplicateProperties = options.AllowDuplicateProperties;
            AllowOutOfOrderMetadataProperties = options.AllowOutOfOrderMetadataProperties;
            AttributeNamingPolicy = options.AttributeNamingPolicy;
            Converters = new XmlConverterList(this, options.Converters);
            DefaultBufferSize = options.DefaultBufferSize;
            DefaultIgnoreCondition = options.DefaultIgnoreCondition;
            DefaultNamespace = options.DefaultNamespace;
            DefaultPropertyMapping = options.DefaultPropertyMapping;
            DictionaryKeyPolicy = options.DictionaryKeyPolicy;
            ElementNamingPolicy = options.ElementNamingPolicy;
            EmitByteOrderMark = options.EmitByteOrderMark;
            EnumHandling = options.EnumHandling;
            IgnoreReadOnlyFields = options.IgnoreReadOnlyFields;
            IgnoreReadOnlyProperties = options.IgnoreReadOnlyProperties;
            IncludeFields = options.IncludeFields;
            IndentCharacter = options.IndentCharacter;
            IndentSize = options.IndentSize;
            InferClosedTypePolymorphism = options.InferClosedTypePolymorphism;
            MaxAttributeCount = options.MaxAttributeCount;
            MaxCommentByteCount = options.MaxCommentByteCount;
            MaxDepth = options.MaxDepth;
            MaxDocumentByteCount = options.MaxDocumentByteCount;
            MaxNameByteCount = options.MaxNameByteCount;
            MaxTextByteCount = options.MaxTextByteCount;
            NewLine = options.NewLine;
            NilHandling = options.NilHandling;
            NumberHandling = options.NumberHandling;
            OmitXmlDeclaration = options.OmitXmlDeclaration;
            PreferredObjectCreationHandling = options.PreferredObjectCreationHandling;
            PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive;
            PropertyNamingPolicy = options.PropertyNamingPolicy;
            ReadCommentHandling = options.ReadCommentHandling;
            ReferenceHandler = options.ReferenceHandler;
            RespectNullableAnnotations = options.RespectNullableAnnotations;
            RespectRequiredConstructorParameters = options.RespectRequiredConstructorParameters;
            UnknownTypeHandling = options.UnknownTypeHandling;
            UnmappedMemberHandling = options.UnmappedMemberHandling;
            WhitespaceHandling = options.WhitespaceHandling;
            WriteIndented = options.WriteIndented;
            XmlVersion = options.XmlVersion;
            _typeInfoResolver = options._typeInfoResolver is XmlTypeInfoResolverChain chain
                ? _typeInfoResolverChain = new XmlTypeInfoResolverChain(this, chain)
                : options._typeInfoResolver;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the converter for <paramref name="typeToConvert"/>.
        /// </summary>
        /// <param name="typeToConvert">The CLR type.</param>
        /// <returns>The converter.</returns>
        /// <exception cref="NotSupportedException">No converter handles <paramref name="typeToConvert"/>.</exception>
        /// <exception cref="InvalidOperationException">An <see cref="XmlConverterAttribute"/> or converter factory is misconfigured.</exception>
        /// <remarks>
        /// The first match wins: <see cref="Converters"/> in list order, then <see cref="XmlConverterAttribute"/>
        /// on the type, then the built-in converters.
        /// </remarks>
        public XmlConverter GetConverter(Type typeToConvert)
        {
            ArgumentNullException.ThrowIfNull(typeToConvert);
            return GetConverterOrNull(typeToConvert)
                ?? throw new NotSupportedException($"The type '{typeToConvert}' is not supported.");
        }

        /// <summary>
        /// Returns the type info for <paramref name="type"/> and makes this instance read-only.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>The type info.</returns>
        /// <exception cref="NotSupportedException"><see cref="TypeInfoResolver"/> has no type info for <paramref name="type"/>.</exception>
        /// <exception cref="InvalidOperationException">No resolver is set and reflection is disabled.</exception>
        public XmlTypeInfo GetTypeInfo(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return GetTypeInfoOrNull(type)
                ?? throw new NotSupportedException($"The type '{type}' is not supported.");
        }

        /// <summary>
        /// Returns the type info for <typeparamref name="T"/> and makes this instance read-only.
        /// </summary>
        /// <typeparam name="T">The CLR type.</typeparam>
        /// <returns>The type info.</returns>
        /// <exception cref="NotSupportedException"><see cref="TypeInfoResolver"/> has no type info for <typeparamref name="T"/>.</exception>
        /// <exception cref="InvalidOperationException">No resolver is set and reflection is disabled.</exception>
        public XmlTypeInfo<T> GetTypeInfo<T>()
        {
            return (XmlTypeInfo<T>)GetTypeInfo(typeof(T));
        }

        /// <summary>
        /// Makes this instance read-only. Every setter and both lists throw <see cref="InvalidOperationException"/> afterwards.
        /// </summary>
        public void MakeReadOnly()
        {
            IsReadOnly = true;
        }

        /// <summary>
        /// Tries to get the type info for <paramref name="type"/>, and makes this instance read-only.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="typeInfo">When this method returns <see langword="true"/>, the type info.</param>
        /// <returns><see langword="false"/> when <see cref="TypeInfoResolver"/> has no type info for <paramref name="type"/>.</returns>
        /// <exception cref="InvalidOperationException">No resolver is set and reflection is disabled.</exception>
        public bool TryGetTypeInfo(Type type, [NotNullWhen(true)] out XmlTypeInfo? typeInfo)
        {
            ArgumentNullException.ThrowIfNull(type);
            typeInfo = GetTypeInfoOrNull(type);
            return typeInfo is not null;
        }

        /// <summary>
        /// Tries to get the type info for <typeparamref name="T"/>, and makes this instance read-only.
        /// </summary>
        /// <typeparam name="T">The CLR type.</typeparam>
        /// <param name="typeInfo">When this method returns <see langword="true"/>, the type info.</param>
        /// <returns><see langword="false"/> when <see cref="TypeInfoResolver"/> has no type info for <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidOperationException">No resolver is set and reflection is disabled.</exception>
        public bool TryGetTypeInfo<T>([NotNullWhen(true)] out XmlTypeInfo<T>? typeInfo)
        {
            typeInfo = (XmlTypeInfo<T>?)GetTypeInfoOrNull(typeof(T));
            return typeInfo is not null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Drops resolved converters so a changed <see cref="Converters"/> list takes effect.
        /// </summary>
        internal void ClearConverterCache()
        {
            _converters.Clear();
        }

        /// <summary>
        /// Converts a CLR member name to an attribute local name.
        /// </summary>
        /// <param name="clrName">The CLR name.</param>
        /// <returns>The XML local name.</returns>
        internal string ConvertAttributeName(string clrName)
        {
            return (AttributeNamingPolicy ?? PropertyNamingPolicy)?.ConvertName(clrName) ?? clrName;
        }

        /// <summary>
        /// Converts a CLR type or member name to an element local name.
        /// </summary>
        /// <param name="clrName">The CLR name.</param>
        /// <returns>The XML local name.</returns>
        internal string ConvertElementName(string clrName)
        {
            return (ElementNamingPolicy ?? PropertyNamingPolicy)?.ConvertName(clrName) ?? clrName;
        }

        /// <summary>
        /// Creates a read-only instance of a named default set.
        /// </summary>
        /// <param name="defaults">The default set.</param>
        /// <returns>The options.</returns>
        internal static XmlSerializerOptions CreateReadOnly(XmlSerializerDefaults defaults)
        {
            var options = new XmlSerializerOptions(defaults);
            options.MakeReadOnly();
            return options;
        }

        /// <summary>
        /// Returns the converter for <paramref name="type"/>, resolving it once per type.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>The converter, or <see langword="null"/> when no converter handles the type.</returns>
        internal XmlConverter? GetConverterOrNull(Type type)
        {
            if (_converters.TryGetValue(type, out var cached))
            {
                return cached;
            }

            var converter = ResolveConverter(type);
            if (converter is not null)
            {
                _converters.TryAdd(type, converter);
            }

            return converter;
        }

        /// <summary>
        /// Returns the type info for <paramref name="type"/>, resolving it once per type, and makes this instance read-only.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>The type info, or <see langword="null"/> when the resolver has none.</returns>
        /// <remarks>
        /// The type info returned last is checked before the dictionary, because most callers serialize one type repeatedly.
        /// A type info found in the dictionary becomes the one returned last, unless a type is being bound at that
        /// moment: one that is still being bound must not be handed to a caller that does not hold the lock.
        /// </remarks>
        internal XmlTypeInfo? GetTypeInfoOrNull(Type type)
        {
            if (_lastTypeInfo is { } last && last.Type == type)
            {
                return last;
            }

            MakeReadOnly();
            lock (_typeInfoLock)
            {
                if (_typeInfos.TryGetValue(type, out var cached))
                {
                    if (_resolveDepth == 0)
                    {
                        _lastTypeInfo = cached;
                    }

                    return cached;
                }

                XmlTypeInfo? typeInfo;
                _resolveDepth++;
                try
                {
                    typeInfo = ResolveTypeInfo(type);
                }
                finally
                {
                    _resolveDepth--;
                }

                _typeInfos.TryAdd(type, typeInfo);
                if (typeInfo is not null)
                {
                    _lastTypeInfo = typeInfo;
                }

                return typeInfo;
            }
        }

        /// <summary>
        /// Stores <paramref name="typeInfo"/> so a recursive request for the same type during binding
        /// returns this instance instead of building it again.
        /// </summary>
        /// <param name="typeInfo">The type info being built.</param>
        internal void CacheTypeInfo(XmlTypeInfo typeInfo)
        {
            _typeInfos[typeInfo.Type] = typeInfo;
        }

        /// <summary>
        /// Drops a type info that failed to finish binding so a later request can throw again.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        internal void RemoveTypeInfo(Type type)
        {
            _typeInfos.TryRemove(type, out _);
        }

        /// <summary>
        /// Finds the converter for <paramref name="type"/> without the cache.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>The converter, or <see langword="null"/> when no converter handles the type.</returns>
        internal XmlConverter? ResolveConverter(Type type)
        {
            foreach (var converter in (XmlConverterList)Converters)
            {
                if (converter.CanConvert(type))
                {
                    return converter.Resolve(type, this);
                }
            }

            if (type.GetCustomAttribute<XmlConverterAttribute>() is not { } attribute)
            {
                return XmlBuiltInConverters.Get(type, this);
            }

            if (!typeof(XmlConverter).IsAssignableFrom(attribute.ConverterType))
            {
                throw new InvalidOperationException($"Type '{attribute.ConverterType}' is not an XmlConverter.");
            }

            return ((XmlConverter)Activator.CreateInstance(attribute.ConverterType)!).Resolve(type, this);
        }

        /// <summary>
        /// Asks <see cref="TypeInfoResolver"/> for the type info of <paramref name="type"/> without the cache.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>The type info, or <see langword="null"/> when the resolver has none.</returns>
        internal XmlTypeInfo? ResolveTypeInfo(Type type)
        {
            var resolver = TypeInfoResolver
                ?? throw new InvalidOperationException("Reflection-based XML serialization is disabled. Set TypeInfoResolver, or use an overload that takes an XmlTypeInfo or XmlSerializerContext.");
            var typeInfo = resolver.GetTypeInfo(type, this);
            if (typeInfo is not null && typeInfo.Type != type)
            {
                throw new InvalidOperationException($"The resolver '{resolver.GetType()}' returned type info for '{typeInfo.Type}' when asked for '{type}'.");
            }

            return typeInfo;
        }

        /// <summary>
        /// Makes <paramref name="chain"/> the resolver after the caller changed it.
        /// </summary>
        /// <param name="chain">The chain owned by this instance.</param>
        internal void SetTypeInfoResolverChain(XmlTypeInfoResolverChain chain)
        {
            _typeInfoResolver = chain;
        }

        /// <summary>
        /// Copies these options onto a reader options struct.
        /// </summary>
        /// <returns>Reader options.</returns>
        internal XmlReaderOptions ToReaderOptions()
        {
            return new XmlReaderOptions
            {
                CommentHandling = ReadCommentHandling,
                MaxAttributeCount = MaxAttributeCount,
                MaxCommentByteCount = MaxCommentByteCount,
                MaxDepth = MaxDepth,
                MaxDocumentByteCount = MaxDocumentByteCount,
                MaxNameByteCount = MaxNameByteCount,
                MaxTextByteCount = MaxTextByteCount,
                WhitespaceHandling = WhitespaceHandling,
            };
        }

        /// <summary>
        /// Copies these options onto a writer options struct.
        /// </summary>
        /// <returns>Writer options.</returns>
        internal XmlWriterOptions ToWriterOptions()
        {
            return new XmlWriterOptions
            {
                EmitByteOrderMark = EmitByteOrderMark,
                IndentCharacter = IndentCharacter,
                IndentSize = IndentSize,
                Indented = WriteIndented,
                MaxAttributeCount = MaxAttributeCount,
                MaxDepth = MaxDepth,
                MaxNameByteCount = MaxNameByteCount,
                NewLine = NewLine,
                OmitXmlDeclaration = OmitXmlDeclaration,
                Version = XmlVersion,
            };
        }

        /// <summary>
        /// Throws when this instance is read-only.
        /// </summary>
        internal void VerifyMutable()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("This XmlSerializerOptions instance is read-only.");
            }
        }

        #endregion

    }

}
