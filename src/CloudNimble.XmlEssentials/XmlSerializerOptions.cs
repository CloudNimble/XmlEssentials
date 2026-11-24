using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Provides options to be used with <see cref="XmlSerializer"/>. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// This class is the XML counterpart of <c>System.Text.Json.JsonSerializerOptions</c>.
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
        /// Gets or sets a value that indicates whether a property can appear more than once in an object being deserialized.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if duplicate properties are allowed; <see langword="false"/> to throw an
        /// <see cref="XmlException"/> when a property appears a second time. The default is <see langword="true"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets or sets the policy used to convert a member's name to an XML attribute name.
        /// </summary>
        /// <value>
        /// A naming policy for attribute names, or <see langword="null"/> to use <see cref="PropertyNamingPolicy"/>.
        /// The default is <see langword="null"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets or sets a value that specifies how the attributes of a start tag are laid out.
        /// </summary>
        /// <value>
        /// One of the enumeration values. The default is <see cref="XmlAttributeStyle.Inline"/>, which writes
        /// every attribute on the line of the start tag.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        /// <remarks>
        /// The value is passed to the writer as <see cref="XmlWriterOptions.AttributeStyle"/>.
        /// </remarks>
        public XmlAttributeStyle AttributeStyle
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of attributes written to a line when <see cref="AttributeStyle"/> is
        /// <see cref="XmlAttributeStyle.PerLine"/>.
        /// </summary>
        /// <value>
        /// The number of attributes to a line. The default is 1. The value is not read when
        /// <see cref="AttributeStyle"/> is <see cref="XmlAttributeStyle.Inline"/>.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is less than 1.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        public int AttributesPerLine
        {
            get;
            set
            {
                VerifyMutable();
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
                field = value;
            }
        } = 1;

        /// <summary>
        /// Gets the list of user-defined converters that were registered.
        /// </summary>
        /// <value>
        /// The list of custom converters.
        /// </value>
        /// <remarks>
        /// A converter in this list is used before an <see cref="XmlConverterAttribute"/> on the type and before the
        /// built-in converters. The list cannot be changed after this instance has become read-only.
        /// </remarks>
        public IList<XmlConverter> Converters { get; }

        /// <summary>
        /// Gets a read-only, singleton instance of <see cref="XmlSerializerOptions"/> that uses the default configuration.
        /// </summary>
        /// <value>
        /// A read-only instance created with <see cref="XmlSerializerDefaults.General"/>.
        /// </value>
        public static XmlSerializerOptions Default { get; } = CreateReadOnly(XmlSerializerDefaults.General);

        /// <summary>
        /// Gets or sets the default buffer size, in bytes, to use when creating temporary buffers.
        /// </summary>
        /// <value>
        /// The default buffer size, in bytes. The default is 0, which means 16,384.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        /// <remarks>
        /// This is the size of the first buffer rented when serializing, and of the first read buffer of
        /// <c>DeserializeAsyncEnumerable</c>.
        /// </remarks>
        public int DefaultBufferSize
        {
            get;
            set
            {
                VerifyMutable();
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that determines when members are ignored during serialization or deserialization.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies when a member is ignored. The default is
        /// <see cref="XmlIgnoreCondition.Never"/>.
        /// </value>
        /// <exception cref="ArgumentException">
        /// The value is <see cref="XmlIgnoreCondition.Always"/>, which would ignore every member.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        /// <remarks>
        /// An <see cref="XmlIgnoreAttribute"/> on a member overrides this value for that member. Every value but
        /// <see cref="XmlIgnoreCondition.Always"/> is accepted. <see cref="XmlIgnoreCondition.WhenWriting"/> and
        /// <see cref="XmlIgnoreCondition.WhenReading"/> have no effect as the default for every member, as in
        /// System.Text.Json: they apply only where an <see cref="XmlIgnoreAttribute"/> gives them.
        /// </remarks>
        public XmlIgnoreCondition DefaultIgnoreCondition
        {
            get;
            set
            {
                VerifyMutable();
                if (value is XmlIgnoreCondition.Always)
                {
                    throw new ArgumentException($"{nameof(DefaultIgnoreCondition)} cannot be {nameof(XmlIgnoreCondition.Always)}, which would ignore every member. Use [XmlIgnore] on the members to leave out.", nameof(value));
                }

                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the namespace URI of the document element for types that do not specify one.
        /// </summary>
        /// <value>
        /// The namespace URI of the document element, or <see langword="null"/> for no namespace. The default is
        /// <see langword="null"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        /// <remarks>
        /// A namespace set on the type with <see cref="XmlRootAttribute"/> takes precedence over this value.
        /// </remarks>
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
        /// Gets or sets a value that specifies how members without a mapping attribute are mapped to XML.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies whether an unannotated member is written as a child element
        /// or as an attribute. The default is <see cref="XmlPropertyMapping.Element"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets or sets the policy used to convert a dictionary key's name to another format, such as camel-casing.
        /// </summary>
        /// <value>
        /// A naming policy for dictionary keys, or <see langword="null"/> to use <see cref="PropertyNamingPolicy"/>.
        /// The default is <see langword="null"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets or sets the policy used to convert a type's or member's name to an XML element name.
        /// </summary>
        /// <value>
        /// A naming policy for element names, or <see langword="null"/> to use <see cref="PropertyNamingPolicy"/>.
        /// The default is <see langword="null"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets or sets a value that indicates whether a UTF-8 byte order mark is written at the start of the output.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if a UTF-8 byte order mark is written; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets or sets a value that specifies how an element that has no content is written.
        /// </summary>
        /// <value>
        /// One of the enumeration values. The default is <see cref="XmlEmptyElementStyle.SelfClosing"/>, which
        /// writes <c>&lt;e/&gt;</c>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        /// <remarks>
        /// The value is passed to the writer as <see cref="XmlWriterOptions.EmptyElementStyle"/>.
        /// </remarks>
        public XmlEmptyElementStyle EmptyElementStyle
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that specifies how enumeration values are written.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies whether an enumeration value is written as its member name
        /// or as its underlying integer. The default is <see cref="XmlEnumHandling.Name"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets or sets a value that indicates whether read-only fields are ignored during serialization and deserialization.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if read-only fields are ignored; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        /// <remarks>
        /// A field is read-only if it is declared <see langword="readonly"/>. Fields are considered only when
        /// <see cref="IncludeFields"/> is <see langword="true"/>.
        /// </remarks>
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
        /// Gets or sets a value that indicates whether read-only properties are ignored during serialization and deserialization.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if read-only properties are ignored; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        /// <remarks>
        /// A property is read-only if it has no <see langword="set"/> or <see langword="init"/> accessor.
        /// </remarks>
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
        /// Gets or sets a value that indicates whether public fields are handled during serialization and deserialization.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if public fields are included; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets or sets the indentation character used when <see cref="WriteIndented"/> is <see langword="true"/>.
        /// </summary>
        /// <value>
        /// The indentation character. The default is the space character.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        public char IndentCharacter
        {
            get;
            set
            {
                VerifyMutable();
                XmlLimits.ThrowIfNotIndentCharacter(value);
                field = value;
            }
        } = ' ';

        /// <summary>
        /// Gets or sets the indentation size used when <see cref="WriteIndented"/> is <see langword="true"/>.
        /// </summary>
        /// <value>
        /// The number of <see cref="IndentCharacter"/> characters written per nesting level. The default is 0, which means 2.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        public int IndentSize
        {
            get;
            set
            {
                VerifyMutable();
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the derived types of a closed type are inferred.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the derived types of a closed type that declares none are taken from its
        /// closed hierarchy; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        /// <remarks>
        /// <para>
        /// A closed type is a class declared with the <see langword="closed"/> modifier, which needs C# 15 and
        /// .NET 11. Its derived types are all in its own assembly, and the compiler lists them. A derived type is
        /// written and read as if the closed type had an <see cref="XmlDerivedTypeAttribute"/> for it, with the
        /// name of the derived type as its discriminator.
        /// </para>
        /// <para>
        /// <see cref="XmlPolymorphicAttribute.InferClosedTypePolymorphism"/> on a type takes precedence over this
        /// value, and a type that declares derived types with <see cref="XmlDerivedTypeAttribute"/> has nothing
        /// inferred. This is the counterpart of <c>JsonSerializerOptions.InferClosedTypePolymorphism</c>.
        /// </para>
        /// </remarks>
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
        /// Gets a value that indicates whether the current instance has been locked for modification.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if this instance can no longer be changed; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// Gets or sets the maximum number of attributes allowed on one element.
        /// </summary>
        /// <value>
        /// The maximum number of attributes on one element. The default is 0, which means 1000.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        public int MaxAttributeCount
        {
            get;
            set
            {
                VerifyMutable();
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum size, in bytes, of the content of a comment that is read.
        /// </summary>
        /// <value>
        /// The maximum size of a comment's content, in bytes. The default is 0, which means 1,048,576.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        public int MaxCommentByteCount
        {
            get;
            set
            {
                VerifyMutable();
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum depth allowed when serializing or deserializing XML.
        /// </summary>
        /// <value>
        /// The maximum element depth allowed when serializing or deserializing XML. The default is 0, which means 64.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        public int MaxDepth
        {
            get;
            set
            {
                VerifyMutable();
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum size, in bytes, of a document that is read.
        /// </summary>
        /// <value>
        /// The maximum document size, in bytes. The default is 0, which means the size is not limited.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        public int MaxDocumentByteCount
        {
            get;
            set
            {
                VerifyMutable();
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum length, in bytes, of an XML name.
        /// </summary>
        /// <value>
        /// The maximum length of a name, in bytes. The default is 0, which means 1024.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        public int MaxNameByteCount
        {
            get;
            set
            {
                VerifyMutable();
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum size, in bytes, of one text node that is read.
        /// </summary>
        /// <value>
        /// The maximum size of a text node, in bytes. The default is 0, which means 16,777,216.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        public int MaxTextByteCount
        {
            get;
            set
            {
                VerifyMutable();
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the namespace declarations that are written on the document element, each as a prefix
        /// and the namespace URI it is bound to.
        /// </summary>
        /// <value>
        /// The declarations, keyed by prefix, in the order they are written, or <see langword="null"/> for none.
        /// An empty prefix is the default namespace. The default is <see langword="null"/>.
        /// </value>
        /// <exception cref="ArgumentException">
        /// The property is set to declarations of which one cannot be written: a prefix that is not an XML name
        /// without a colon, the <c>xmlns</c> prefix, the <c>xml</c> prefix or its namespace bound to anything but
        /// each other, the <c>xmlns</c> namespace, or a namespace URI that is <see langword="null"/> or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        /// <example>
        /// <code>
        /// var options = new XmlSerializerOptions { Namespaces = KnownNamespaces.Podcast };
        /// string xml = XmlSerializer.Serialize(feed, options);
        /// </code>
        /// </example>
        /// <remarks>
        /// <para>
        /// Without this property the serializer declares a namespace where it is first needed and chooses how:
        /// as the default namespace of an element, or with a generated prefix such as <c>ns1</c> for an attribute.
        /// A namespace that is declared here is in scope for the whole document, so every element and attribute
        /// in it is written with the prefix given here. A declaration is written whether or not anything uses it.
        /// When two prefixes are bound to one namespace, the one declared last is used. The declarations are
        /// written in the order given, with one exception: the declaration that names the namespace of the
        /// document element itself comes first, because the element's start tag needs it.
        /// </para>
        /// <para>
        /// The declarations are copied when the property is set. Use
        /// <see cref="XmlDocument.DiscoverNamespaces(ReadOnlySpan{byte}, XmlDocumentOptions)"/> to take them from
        /// an existing document, <see cref="KnownNamespaces"/> for those of a well-known format, or
        /// <see cref="XmlNamespaceDeclarationAttribute"/> to state them on the type. These declarations take
        /// precedence over the type's.
        /// </para>
        /// <para>
        /// An empty prefix declares the default namespace on the document element, so an element with no
        /// namespace of its own is in that namespace. When the document element has a namespace of its own that
        /// is not this one and has no prefix declared, the two cannot both be written, and serialization throws an
        /// <see cref="InvalidOperationException"/>.
        /// </para>
        /// <para>
        /// The property affects writing only. When the serializer reads, it compares namespace URIs and never
        /// prefixes.
        /// </para>
        /// </remarks>
        public IReadOnlyDictionary<string, string>? Namespaces
        {
            get;
            set
            {
                VerifyMutable();
                field = value is null ? null : XmlNamespaceDeclarations.Copy(value);
            }
        }

        /// <summary>
        /// Gets or sets the line ending that is written.
        /// </summary>
        /// <value>
        /// The line ending: <c>"\n"</c> or <c>"\r\n"</c>. The default is the value of <see cref="Environment.NewLine"/>.
        /// </value>
        /// <exception cref="ArgumentNullException">
        /// The property is set to <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is neither <c>"\n"</c> nor <c>"\r\n"</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        public string NewLine
        {
            get;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                XmlLimits.ThrowIfNotNewLine(value);
                VerifyMutable();
                field = value;
            }
        } = Environment.NewLine;

        /// <summary>
        /// Gets or sets a value that specifies whether the named floating-point literals <c>INF</c>, <c>-INF</c>, and <c>NaN</c> are read and written.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies how named floating-point literals are handled. The default is
        /// <see cref="XmlNumberHandling.AllowNamedFloatingPointLiterals"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets or sets a value that indicates whether the XML declaration is omitted from the output.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the XML declaration is omitted; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets or sets the preferred object creation handling for members when deserializing XML.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies whether a member that already holds a value is given a new
        /// one or has the value it holds populated. The default is <see cref="XmlObjectCreationHandling.Replace"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The rules are those of <c>JsonSerializerOptions.PreferredObjectCreationHandling</c>. Under
        /// <see cref="XmlObjectCreationHandling.Replace"/> a member with no setter is not read. Under
        /// <see cref="XmlObjectCreationHandling.Populate"/> the items of a collection and the entries of a dictionary
        /// are added to the instance the member holds, and the members of an object are set on it, whether or not the
        /// member has a setter. A member that holds <see langword="null"/> is given a new instance when it has a setter.
        /// </para>
        /// <para>
        /// The option applies only where a member can be populated. An array, an immutable or read-only collection, a
        /// polymorphic type, a value with a custom converter, a value type with no setter, and every member of a type
        /// that is created through a constructor with parameters are handled as under
        /// <see cref="XmlObjectCreationHandling.Replace"/>. <see cref="XmlObjectCreationHandlingAttribute"/> sets the
        /// handling of one member or one type and takes precedence.
        /// </para>
        /// <para>
        /// <see cref="XmlObjectCreationHandling.Populate"/> cannot be combined with <see cref="ReferenceHandler"/>: a
        /// type with a member to populate throws an <see cref="InvalidOperationException"/> when it is first used.
        /// </para>
        /// </remarks>
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
        /// Gets or sets a value that indicates whether a property's name uses a case-insensitive comparison during deserialization.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if property names are compared case-insensitively; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets or sets the policy used to convert a type's or member's name to another format, such as camel-casing, when no more specific policy is set.
        /// </summary>
        /// <value>
        /// A naming policy, or <see langword="null"/> to leave names unchanged. The default is <see langword="null"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        /// <remarks>
        /// <see cref="ElementNamingPolicy"/>, <see cref="AttributeNamingPolicy"/>, and <see cref="DictionaryKeyPolicy"/>
        /// each fall back to this policy when they are <see langword="null"/>.
        /// </remarks>
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
        /// Gets or sets a value that defines how comments are handled during deserialization.
        /// </summary>
        /// <value>
        /// One of the enumeration values that indicates whether comments are allowed, disallowed, or skipped.
        /// The default is <see cref="XmlCommentHandling.Skip"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets or sets an object that specifies how object references are handled when reading and writing XML.
        /// </summary>
        /// <value>
        /// The reference handler, or <see langword="null"/> if object references are not tracked. The default is
        /// <see langword="null"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets or sets a value that indicates whether a <see langword="null"/> is refused for a member or constructor parameter annotated non-nullable.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if nullability annotations are respected; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        /// <remarks>
        /// <para>
        /// When this is <see langword="true"/>, an element marked <c>xsi:nil="true"</c> for a member or constructor
        /// parameter that does not accept <see langword="null"/> throws an <see cref="XmlException"/> when it is read, and
        /// a member annotated as never returning <see langword="null"/> that holds one throws when it is written. A
        /// <see langword="null"/> that the ignore condition leaves out is not written and is not refused.
        /// </para>
        /// <para>
        /// The rules are those of <c>JsonSerializerOptions.RespectNullableAnnotations</c>. The annotation of the member
        /// decides, together with <c>[AllowNull]</c>, <c>[DisallowNull]</c>, <c>[MaybeNull]</c> and <c>[NotNull]</c>. A
        /// member the document leaves out is not checked. The items of a collection, the values of a dictionary, the
        /// document root, a member compiled without nullable annotations, and a member whose type is an unconstrained
        /// type parameter are not checked either.
        /// </para>
        /// </remarks>
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
        /// Gets or sets a value that indicates whether non-optional constructor parameters must be present in the XML during deserialization.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if an <see cref="XmlException"/> is thrown when the XML has no value for a constructor
        /// parameter that has no default value; otherwise, <see langword="false"/>. The default is <see langword="true"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets a read-only, singleton instance of <see cref="XmlSerializerOptions"/> that uses the strict configuration.
        /// </summary>
        /// <value>
        /// A read-only instance created with <see cref="XmlSerializerDefaults.Strict"/>.
        /// </value>
        public static XmlSerializerOptions Strict { get; } = CreateReadOnly(XmlSerializerDefaults.Strict);

        /// <summary>
        /// Gets or sets the <see cref="XmlTypeInfo"/> contract resolver used by this instance.
        /// </summary>
        /// <value>
        /// The resolver that supplies <see cref="XmlTypeInfo"/> metadata, or <see langword="null"/> if none is set and
        /// reflection-based serialization is disabled.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets the list of chained <see cref="XmlTypeInfo"/> contract resolvers used by this instance.
        /// </summary>
        /// <value>
        /// The resolvers that make up <see cref="TypeInfoResolver"/>, in the order they are asked.
        /// </value>
        /// <remarks>
        /// The list is empty until a resolver has been set. Changing the list makes it the
        /// <see cref="TypeInfoResolver"/>; setting <see cref="TypeInfoResolver"/> replaces the list's content.
        /// </remarks>
        public IList<IXmlTypeInfoResolver> TypeInfoResolverChain => _typeInfoResolverChain ??= new XmlTypeInfoResolverChain(this, _typeInfoResolver);

        /// <summary>
        /// Gets or sets a value that specifies how an element is read into a value declared as <see cref="object"/>.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies whether the element is read as an <see cref="XmlElement"/> or
        /// as an <see cref="Nodes.XmlNode"/>. The default is <see cref="XmlUnknownTypeHandling.XmlElement"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        /// <remarks>
        /// This is the counterpart of <c>JsonSerializerOptions.UnknownTypeHandling</c>. It applies wherever the
        /// declared type is <see cref="object"/>: a member, an item of a collection, a value of a dictionary, and the
        /// document root. An element marked <c>xsi:nil="true"</c> is read as <see langword="null"/> under both values.
        /// Writing is not affected: a value declared as <see cref="object"/> is written as the type it has.
        /// </remarks>
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
        /// Gets or sets a value that specifies how XML elements and attributes that cannot be mapped to a member are handled during deserialization.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies whether an unmapped element or attribute is skipped or
        /// rejected. The default is <see cref="XmlUnmappedMemberHandling.Skip"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets a read-only, singleton instance of <see cref="XmlSerializerOptions"/> that uses the web configuration.
        /// </summary>
        /// <value>
        /// A read-only instance created with <see cref="XmlSerializerDefaults.Web"/>: camel-case names and no XML declaration.
        /// </value>
        public static XmlSerializerOptions Web { get; } = CreateReadOnly(XmlSerializerDefaults.Web);

        /// <summary>
        /// Gets or sets a value that specifies how white-space-only text between elements is handled during deserialization.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies how the reader treats white-space-only text between elements.
        /// The default is <see cref="XmlWhitespaceHandling.Insignificant"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
        public XmlWhitespaceHandling WhitespaceHandling
        {
            get;
            set
            {
                VerifyMutable();
                field = value;
            }
        } = XmlWhitespaceHandling.Insignificant;

        /// <summary>
        /// Gets or sets a value that indicates whether XML should use pretty printing. By default, XML is serialized without any extra white space.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if XML is pretty printed on serialization; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Gets or sets the XML version of the output, which is written in the XML declaration.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies the XML version of the output. The default is
        /// <see cref="CloudNimble.Text.Xml.XmlVersion.Version10"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The property is set after this instance has become read-only.
        /// </exception>
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
        /// Initializes a new instance of the <see cref="XmlSerializerOptions"/> class.
        /// </summary>
        /// <remarks>
        /// The new instance uses the <see cref="XmlSerializerDefaults.General"/> configuration.
        /// </remarks>
        public XmlSerializerOptions()
        {
            Converters = new XmlConverterList(this, []);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlSerializerOptions"/> class with a predefined set of options determined by the specified <see cref="XmlSerializerDefaults"/>.
        /// </summary>
        /// <param name="defaults">One of the enumeration values that specifies the predefined set of options to use.</param>
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
                RespectNullableAnnotations = true;
                ReadCommentHandling = XmlCommentHandling.Disallow;
                UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlSerializerOptions"/> class by copying the options from another instance.
        /// </summary>
        /// <param name="options">The options instance to copy options from.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// The copy is not read-only and shares no cached state with <paramref name="options"/>.
        /// </remarks>
        public XmlSerializerOptions(XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            AllowDuplicateProperties = options.AllowDuplicateProperties;
            AttributeNamingPolicy = options.AttributeNamingPolicy;
            Converters = new XmlConverterList(this, options.Converters);
            DefaultBufferSize = options.DefaultBufferSize;
            DefaultIgnoreCondition = options.DefaultIgnoreCondition;
            DefaultNamespace = options.DefaultNamespace;
            Namespaces = options.Namespaces;
            AttributeStyle = options.AttributeStyle;
            AttributesPerLine = options.AttributesPerLine;
            DefaultPropertyMapping = options.DefaultPropertyMapping;
            DictionaryKeyPolicy = options.DictionaryKeyPolicy;
            ElementNamingPolicy = options.ElementNamingPolicy;
            EmitByteOrderMark = options.EmitByteOrderMark;
            EmptyElementStyle = options.EmptyElementStyle;
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
        /// Returns the converter for the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type to return a converter for.</param>
        /// <returns>
        /// The first converter that supports the given type.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="typeToConvert"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="XmlConverter"/> for <paramref name="typeToConvert"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// An <see cref="XmlConverterAttribute"/> or a converter factory for <paramref name="typeToConvert"/> is misconfigured.
        /// </exception>
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
        /// Gets the <see cref="XmlTypeInfo"/> contract metadata resolved by the current <see cref="XmlSerializerOptions"/> instance for the specified type, and makes this instance read-only.
        /// </summary>
        /// <param name="type">The type to resolve contract metadata for.</param>
        /// <returns>
        /// The contract metadata resolved for <paramref name="type"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <see cref="TypeInfoResolver"/> has no contract metadata for <paramref name="type"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No resolver is set and reflection-based serialization is disabled.
        /// -or-
        /// The resolver returned contract metadata for a type other than <paramref name="type"/>.
        /// </exception>
        public XmlTypeInfo GetTypeInfo(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return GetTypeInfoOrNull(type)
                ?? throw new NotSupportedException($"The type '{type}' is not supported.");
        }

        /// <summary>
        /// Gets the <see cref="XmlTypeInfo{T}"/> contract metadata resolved by the current <see cref="XmlSerializerOptions"/> instance for the type specified by a generic type parameter, and makes this instance read-only.
        /// </summary>
        /// <typeparam name="T">
        /// The type to resolve contract metadata for.
        /// </typeparam>
        /// <returns>
        /// The contract metadata resolved for <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// <see cref="TypeInfoResolver"/> has no contract metadata for <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No resolver is set and reflection-based serialization is disabled.
        /// -or-
        /// The resolver returned contract metadata for a type other than <typeparamref name="T"/>.
        /// </exception>
        public XmlTypeInfo<T> GetTypeInfo<T>()
        {
            return (XmlTypeInfo<T>)GetTypeInfo(typeof(T));
        }

        /// <summary>
        /// Marks the current instance as read-only to prevent any further user modification.
        /// </summary>
        /// <remarks>
        /// Afterwards, every property setter and any change to <see cref="Converters"/> or
        /// <see cref="TypeInfoResolverChain"/> throws an <see cref="InvalidOperationException"/>.
        /// </remarks>
        public void MakeReadOnly()
        {
            IsReadOnly = true;
        }

        /// <summary>
        /// Tries to get the <see cref="XmlTypeInfo"/> contract metadata resolved by the current <see cref="XmlSerializerOptions"/> instance for the specified type, and makes this instance read-only.
        /// </summary>
        /// <param name="type">The type to resolve contract metadata for.</param>
        /// <param name="typeInfo">When this method returns, contains the resolved contract metadata, or <see langword="null"/> if <see cref="TypeInfoResolver"/> has none for <paramref name="type"/>. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the contract metadata for <paramref name="type"/> was resolved; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No resolver is set and reflection-based serialization is disabled.
        /// -or-
        /// The resolver returned contract metadata for a type other than <paramref name="type"/>.
        /// </exception>
        public bool TryGetTypeInfo(Type type, [NotNullWhen(true)] out XmlTypeInfo? typeInfo)
        {
            ArgumentNullException.ThrowIfNull(type);
            typeInfo = GetTypeInfoOrNull(type);
            return typeInfo is not null;
        }

        /// <summary>
        /// Tries to get the <see cref="XmlTypeInfo{T}"/> contract metadata resolved by the current <see cref="XmlSerializerOptions"/> instance for the type specified by a generic type parameter, and makes this instance read-only.
        /// </summary>
        /// <typeparam name="T">
        /// The type to resolve contract metadata for.
        /// </typeparam>
        /// <param name="typeInfo">When this method returns, contains the resolved contract metadata, or <see langword="null"/> if <see cref="TypeInfoResolver"/> has none for <typeparamref name="T"/>. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// <see langword="true"/> if the contract metadata for <typeparamref name="T"/> was resolved; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// No resolver is set and reflection-based serialization is disabled.
        /// -or-
        /// The resolver returned contract metadata for a type other than <typeparamref name="T"/>.
        /// </exception>
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
        /// Returns whether the caller supplied a converter for <paramref name="type"/>, in
        /// <see cref="Converters"/> or with <see cref="XmlConverterAttribute"/> on the type.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns><see langword="true"/> if a converter other than a built-in one handles the type.</returns>
        internal bool HasCustomConverter(Type type)
        {
            foreach (var converter in (XmlConverterList)Converters)
            {
                if (converter.CanConvert(type))
                {
                    return true;
                }
            }

            return type.GetCustomAttribute<XmlConverterAttribute>() is not null;
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

            return attribute.CreateConverter().Resolve(type, this);
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
                AttributeStyle = AttributeStyle,
                AttributesPerLine = AttributesPerLine,
                EmitByteOrderMark = EmitByteOrderMark,
                EmptyElementStyle = EmptyElementStyle,
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
