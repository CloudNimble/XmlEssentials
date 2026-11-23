# XmlSerializer

Part of the [XmlEssentials spec](README.md).

**This page is the authoritative overload list and the copy-paste public surface** for `XmlSerializer`, `XmlSerializerOptions`, type info, context, and source-gen attributes. Every destination that has `TValue` + `XmlTypeInfo<T>` also has `TValue` + `XmlSerializerContext` (serialize **and** deserialize). Reflection overloads (`options` or `object`+`Type` without context/typeInfo) carry `[RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]`.

Constants: `RefMsg` / `DynMsg` match STJ wording (reflection trim/AOT).

## Enums

```csharp
namespace CloudNimble.Text.Xml;

public enum XmlNilHandling : byte
{
    WriteXsiNil = 0,
    WriteEmptyElement = 1,
    Omit = 2,
}

public enum XmlEnumHandling : byte
{
    Name = 0,
    Integer = 1,
}

public enum XmlDiscriminatorPlacement : byte
{
    ElementName = 0,
    Attribute = 1,
    XsiType = 2,
}

public enum XmlPropertyMapping : byte
{
    Element = 0,
    Attribute = 1,
}

namespace CloudNimble.Text.Xml.Serialization;

public enum XmlIgnoreCondition
{
    Never = 0,
    Always = 1,
    WhenWritingDefault = 2,
    WhenWritingNull = 3,
}

public enum XmlUnmappedMemberHandling : byte
{
    Skip = 0,
    Disallow = 1,
}

[Flags]
public enum XmlNumberHandling
{
    Strict = 0,
    AllowNamedFloatingPointLiterals = 1,
}

public enum XmlKnownNamingPolicy
{
    Unspecified = 0,
    CamelCase = 1,
    SnakeCaseLower = 2,
    SnakeCaseUpper = 3,
    KebabCaseLower = 4,
    KebabCaseUpper = 5,
    PascalCase = 6,
}

[Flags]
public enum XmlSourceGenerationMode
{
    Default = 0,       // Metadata | Serialization (STJ)
    Metadata = 1,
    Serialization = 2,
}

public enum XmlObjectCreationHandling
{
    Replace = 0,
    Populate = 1,
}

public enum XmlUnknownTypeHandling
{
    XmlElement = 0,
    XmlNode = 1,
}
```

`XmlSerializerOptions.NumberHandling` default is `AllowNamedFloatingPointLiterals`. `Strict` rejects INF/NaN. No `WriteAsString` / `AllowReadingFromString` ([K33](01-identity-and-decisions.md#key-decisions)). Lexical: [02](02-xml-fidelity.md#lexical-rules-xsd-10-invariant) (`decimal` **rejects exponent**).

## Naming policies

```csharp
namespace CloudNimble.Text.Xml;

public abstract class XmlNamingPolicy
{
    protected XmlNamingPolicy();

    public static XmlNamingPolicy CamelCase { get; }
    public static XmlNamingPolicy PascalCase { get; }
    public static XmlNamingPolicy KebabCaseLower { get; }
    public static XmlNamingPolicy KebabCaseUpper { get; }
    public static XmlNamingPolicy SnakeCaseLower { get; }
    public static XmlNamingPolicy SnakeCaseUpper { get; }

    public abstract string ConvertName(string name);
}
```

Identity is `null` on options (match STJ).

## Options

```csharp
namespace CloudNimble.Text.Xml;

public sealed class XmlSerializerOptions
{
    public XmlSerializerOptions();
    public XmlSerializerOptions(XmlSerializerDefaults defaults);
    public XmlSerializerOptions(XmlSerializerOptions options);

    public static XmlSerializerOptions Default { get; }
    public static XmlSerializerOptions Web { get; }
    public static XmlSerializerOptions Strict { get; }

    public bool WriteIndented { get; set; }
    public char IndentCharacter { get; set; }
    public int IndentSize { get; set; }
    public string NewLine { get; set; }

    public int MaxDepth { get; set; }
    public int DefaultBufferSize { get; set; }
    public int MaxAttributeCount { get; set; }
    public int MaxNameByteCount { get; set; }
    public int MaxCommentByteCount { get; set; }
    public int MaxTextByteCount { get; set; }
    public int MaxDocumentByteCount { get; set; }

    public bool IgnoreReadOnlyProperties { get; set; }
    public bool IgnoreReadOnlyFields { get; set; }
    public bool IncludeFields { get; set; }
    public bool PropertyNameCaseInsensitive { get; set; }
    public XmlIgnoreCondition DefaultIgnoreCondition { get; set; }
    public XmlPropertyMapping DefaultPropertyMapping { get; set; }
    public XmlNumberHandling NumberHandling { get; set; }
    public XmlUnmappedMemberHandling UnmappedMemberHandling { get; set; }
    public XmlObjectCreationHandling PreferredObjectCreationHandling { get; set; }
    public XmlUnknownTypeHandling UnknownTypeHandling { get; set; }
    public XmlCommentHandling ReadCommentHandling { get; set; }
    public bool RespectNullableAnnotations { get; set; }
    public bool RespectRequiredConstructorParameters { get; set; }

    public XmlNamingPolicy? PropertyNamingPolicy { get; set; }
    public XmlNamingPolicy? DictionaryKeyPolicy { get; set; }
    public IList<XmlConverter> Converters { get; }
    public IXmlTypeInfoResolver? TypeInfoResolver { get; set; }
    public IList<IXmlTypeInfoResolver> TypeInfoResolverChain { get; }
    public XmlConverter GetConverter(Type typeToConvert);
    public XmlReferenceHandler? ReferenceHandler { get; set; }
    public bool AllowDuplicateProperties { get; set; }
    public bool AllowOutOfOrderMetadataProperties { get; set; }
    public bool InferClosedTypePolymorphism { get; set; }

    public bool IsReadOnly { get; }
    public void MakeReadOnly();
    public XmlTypeInfo GetTypeInfo(Type type);
    public XmlTypeInfo<T> GetTypeInfo<T>();
    public bool TryGetTypeInfo(Type type, [NotNullWhen(true)] out XmlTypeInfo? typeInfo);
    public bool TryGetTypeInfo<T>([NotNullWhen(true)] out XmlTypeInfo<T>? typeInfo);

    public bool OmitXmlDeclaration { get; set; }
    public bool EmitByteOrderMark { get; set; }
    public XmlVersion XmlVersion { get; set; }
    public string? DefaultNamespace { get; set; }
    public XmlNamingPolicy? ElementNamingPolicy { get; set; }
    public XmlNamingPolicy? AttributeNamingPolicy { get; set; }
    public XmlNilHandling NilHandling { get; set; }
    public XmlEnumHandling EnumHandling { get; set; }
    public XmlWhitespaceHandling WhitespaceHandling { get; set; }
}

public enum XmlSerializerDefaults
{
    General = 0,
    Web = 1,
    Strict = 2,
}
```

Defaults (when unset / `0` on max-* except `MaxDocumentByteCount`):

| Property | Default |
| --- | --- |
| `WriteIndented` | `false` |
| `IndentCharacter` | `' '` |
| `IndentSize` | `2` |
| `NewLine` | `Environment.NewLine` |
| `MaxDepth` | `0` → 64 |
| `DefaultBufferSize` | `0` → 16384 |
| `MaxAttributeCount` | `0` → 1000 |
| `MaxNameByteCount` | `0` → 1024 |
| `MaxCommentByteCount` | `0` → 1_048_576 |
| `MaxTextByteCount` | `0` → 16_777_216 |
| `MaxDocumentByteCount` | `0` = unbounded |
| `PropertyNameCaseInsensitive` | `false` |
| `NumberHandling` | `AllowNamedFloatingPointLiterals` |
| `RespectNullableAnnotations` | `false` |
| `RespectRequiredConstructorParameters` | `true` ([K37](01-identity-and-decisions.md#key-decisions)) |
| `DefaultPropertyMapping` | `Element` ([K7](01-identity-and-decisions.md#key-decisions)). `Attribute` writes unannotated simple members as XML attributes; nested objects and collections stay elements. `[XmlElement]` / `[XmlAttribute]` / `[XmlText]` override per member. |
| `TypeInfoResolver` | `DefaultXmlTypeInfoResolver.Instance` when reflection is enabled |
| `WhitespaceHandling` | forwarded to `Utf8XmlReader` on deserialize only ([K38](01-identity-and-decisions.md#key-decisions)) |
| `General` / `Default` | identity names, write declaration, xsi:nil for null |
| `Web` | CamelCase, `OmitXmlDeclaration = true` |
| `Strict` | `UnmappedMemberHandling.Disallow`, `AllowDuplicateProperties = false`, comments `Disallow` |

Effective name policy: member kind chooses `AttributeNamingPolicy` or `ElementNamingPolicy`, each falling back to `PropertyNamingPolicy`, which falling back to identity.

## XmlSerializer (complete)

```csharp
namespace CloudNimble.Text.Xml;

public static class XmlSerializer
{
    public static bool IsReflectionEnabledByDefault { get; }

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static string Serialize<TValue>(TValue value, XmlSerializerOptions? options = null);
    public static string Serialize<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo);
    public static string Serialize<TValue>(TValue value, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static string Serialize(object? value, Type inputType, XmlSerializerOptions? options = null);
    public static string Serialize(object? value, Type inputType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, XmlSerializerOptions? options = null);
    public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo);
    public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static byte[] SerializeToUtf8Bytes(object? value, Type inputType, XmlSerializerOptions? options = null);
    public static byte[] SerializeToUtf8Bytes(object? value, Type inputType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static void Serialize<TValue>(Utf8XmlWriter writer, TValue value, XmlSerializerOptions? options = null);
    public static void Serialize<TValue>(Utf8XmlWriter writer, TValue value, XmlTypeInfo<TValue> xmlTypeInfo);
    public static void Serialize<TValue>(Utf8XmlWriter writer, TValue value, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static void Serialize(Utf8XmlWriter writer, object? value, Type inputType, XmlSerializerOptions? options = null);
    public static void Serialize(Utf8XmlWriter writer, object? value, Type inputType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static void Serialize<TValue>(Stream utf8Xml, TValue value, XmlSerializerOptions? options = null);
    public static void Serialize<TValue>(Stream utf8Xml, TValue value, XmlTypeInfo<TValue> xmlTypeInfo);
    public static void Serialize<TValue>(Stream utf8Xml, TValue value, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static void Serialize(Stream utf8Xml, object? value, Type inputType, XmlSerializerOptions? options = null);
    public static void Serialize(Stream utf8Xml, object? value, Type inputType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static void Serialize<TValue>(IBufferWriter<byte> bufferWriter, TValue value, XmlSerializerOptions? options = null);
    public static void Serialize<TValue>(IBufferWriter<byte> bufferWriter, TValue value, XmlTypeInfo<TValue> xmlTypeInfo);
    public static void Serialize<TValue>(IBufferWriter<byte> bufferWriter, TValue value, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static void Serialize(IBufferWriter<byte> bufferWriter, object? value, Type inputType, XmlSerializerOptions? options = null);
    public static void Serialize(IBufferWriter<byte> bufferWriter, object? value, Type inputType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static Task SerializeAsync<TValue>(Stream utf8Xml, TValue value, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default);
    public static Task SerializeAsync<TValue>(Stream utf8Xml, TValue value, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default);
    public static Task SerializeAsync<TValue>(Stream utf8Xml, TValue value, XmlSerializerContext context, CancellationToken cancellationToken = default);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static Task SerializeAsync(Stream utf8Xml, object? value, Type inputType, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default);
    public static Task SerializeAsync(Stream utf8Xml, object? value, Type inputType, XmlSerializerContext context, CancellationToken cancellationToken = default);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static XmlDocument SerializeToDocument<TValue>(TValue value, XmlSerializerOptions? options = null);
    public static XmlDocument SerializeToDocument<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo);
    public static XmlDocument SerializeToDocument<TValue>(TValue value, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static XmlDocument SerializeToDocument(object? value, Type inputType, XmlSerializerOptions? options = null);
    public static XmlDocument SerializeToDocument(object? value, Type inputType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static XmlElement SerializeToElement<TValue>(TValue value, XmlSerializerOptions? options = null);
    public static XmlElement SerializeToElement<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo);
    public static XmlElement SerializeToElement<TValue>(TValue value, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static XmlElement SerializeToElement(object? value, Type inputType, XmlSerializerOptions? options = null);
    public static XmlElement SerializeToElement(object? value, Type inputType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static Nodes.XmlNode? SerializeToNode<TValue>(TValue value, XmlSerializerOptions? options = null);
    public static Nodes.XmlNode? SerializeToNode<TValue>(TValue value, XmlTypeInfo<TValue> xmlTypeInfo);
    public static Nodes.XmlNode? SerializeToNode<TValue>(TValue value, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static Nodes.XmlNode? SerializeToNode(object? value, Type inputType, XmlSerializerOptions? options = null);
    public static Nodes.XmlNode? SerializeToNode(object? value, Type inputType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static TValue? Deserialize<TValue>(string xml, XmlSerializerOptions? options = null);
    public static TValue? Deserialize<TValue>(string xml, XmlTypeInfo<TValue> xmlTypeInfo);
    public static TValue? Deserialize<TValue>(string xml, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static object? Deserialize(string xml, Type returnType, XmlSerializerOptions? options = null);
    public static object? Deserialize(string xml, Type returnType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static TValue? Deserialize<TValue>(ReadOnlySpan<char> xml, XmlSerializerOptions? options = null);
    public static TValue? Deserialize<TValue>(ReadOnlySpan<char> xml, XmlTypeInfo<TValue> xmlTypeInfo);
    public static TValue? Deserialize<TValue>(ReadOnlySpan<char> xml, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static object? Deserialize(ReadOnlySpan<char> xml, Type returnType, XmlSerializerOptions? options = null);
    public static object? Deserialize(ReadOnlySpan<char> xml, Type returnType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Xml, XmlSerializerOptions? options = null);
    public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo);
    public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Xml, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static object? Deserialize(ReadOnlySpan<byte> utf8Xml, Type returnType, XmlSerializerOptions? options = null);
    public static object? Deserialize(ReadOnlySpan<byte> utf8Xml, Type returnType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static TValue? Deserialize<TValue>(ReadOnlySequence<byte> utf8Xml, XmlSerializerOptions? options = null);
    public static TValue? Deserialize<TValue>(ReadOnlySequence<byte> utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo);
    public static TValue? Deserialize<TValue>(ReadOnlySequence<byte> utf8Xml, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static object? Deserialize(ReadOnlySequence<byte> utf8Xml, Type returnType, XmlSerializerOptions? options = null);
    public static object? Deserialize(ReadOnlySequence<byte> utf8Xml, Type returnType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static TValue? Deserialize<TValue>(ref Utf8XmlReader reader, XmlSerializerOptions? options = null);
    public static TValue? Deserialize<TValue>(ref Utf8XmlReader reader, XmlTypeInfo<TValue> xmlTypeInfo);
    public static TValue? Deserialize<TValue>(ref Utf8XmlReader reader, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static object? Deserialize(ref Utf8XmlReader reader, Type returnType, XmlSerializerOptions? options = null);
    public static object? Deserialize(ref Utf8XmlReader reader, Type returnType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static TValue? Deserialize<TValue>(Stream utf8Xml, XmlSerializerOptions? options = null);
    public static TValue? Deserialize<TValue>(Stream utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo);
    public static TValue? Deserialize<TValue>(Stream utf8Xml, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static object? Deserialize(Stream utf8Xml, Type returnType, XmlSerializerOptions? options = null);
    public static object? Deserialize(Stream utf8Xml, Type returnType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static ValueTask<TValue?> DeserializeAsync<TValue>(Stream utf8Xml, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default);
    public static ValueTask<TValue?> DeserializeAsync<TValue>(Stream utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default);
    public static ValueTask<TValue?> DeserializeAsync<TValue>(Stream utf8Xml, XmlSerializerContext context, CancellationToken cancellationToken = default);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static ValueTask<object?> DeserializeAsync(Stream utf8Xml, Type returnType, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default);
    public static ValueTask<object?> DeserializeAsync(Stream utf8Xml, Type returnType, XmlSerializerContext context, CancellationToken cancellationToken = default);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static Task SerializeAsync<TValue>(PipeWriter utf8Xml, TValue value, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static ValueTask<TValue?> DeserializeAsync<TValue>(PipeReader utf8Xml, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default);
    public static ValueTask<TValue?> DeserializeAsync<TValue>(PipeReader utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream utf8Xml, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream utf8Xml, bool topLevelValues, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default);
    public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default);
    public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo, bool topLevelValues, CancellationToken cancellationToken = default);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(PipeReader utf8Xml, bool topLevelValues, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default);
    public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(PipeReader utf8Xml, XmlTypeInfo<TValue> xmlTypeInfo, CancellationToken cancellationToken = default);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static Task SerializeAsyncEnumerable<TValue>(Stream utf8Xml, IAsyncEnumerable<TValue> values, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static Task SerializeAsyncEnumerable<TValue>(Stream utf8Xml, IAsyncEnumerable<TValue> values, bool topLevelValues, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default);
    public static Task SerializeAsyncEnumerable<TValue>(Stream utf8Xml, IAsyncEnumerable<TValue> values, XmlTypeInfo<TValue> xmlTypeInfo, bool topLevelValues = false, CancellationToken cancellationToken = default);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static Task SerializeAsyncEnumerable<TValue>(PipeWriter utf8Xml, IAsyncEnumerable<TValue> values, bool topLevelValues, XmlSerializerOptions? options = null, CancellationToken cancellationToken = default);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static TValue? Deserialize<TValue>(XmlDocument document, XmlSerializerOptions? options = null);
    public static TValue? Deserialize<TValue>(XmlDocument document, XmlTypeInfo<TValue> xmlTypeInfo);
    public static TValue? Deserialize<TValue>(XmlDocument document, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static object? Deserialize(XmlDocument document, Type returnType, XmlSerializerOptions? options = null);
    public static object? Deserialize(XmlDocument document, Type returnType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static TValue? Deserialize<TValue>(XmlElement element, XmlSerializerOptions? options = null);
    public static TValue? Deserialize<TValue>(XmlElement element, XmlTypeInfo<TValue> xmlTypeInfo);
    public static TValue? Deserialize<TValue>(XmlElement element, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static object? Deserialize(XmlElement element, Type returnType, XmlSerializerOptions? options = null);
    public static object? Deserialize(XmlElement element, Type returnType, XmlSerializerContext context);

    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static TValue? Deserialize<TValue>(Nodes.XmlNode node, XmlSerializerOptions? options = null);
    public static TValue? Deserialize<TValue>(Nodes.XmlNode node, XmlTypeInfo<TValue> xmlTypeInfo);
    public static TValue? Deserialize<TValue>(Nodes.XmlNode node, XmlSerializerContext context);
    [RequiresUnreferencedCode(RefMsg), RequiresDynamicCode(DynMsg)]
    public static object? Deserialize(Nodes.XmlNode node, Type returnType, XmlSerializerOptions? options = null);
    public static object? Deserialize(Nodes.XmlNode node, Type returnType, XmlSerializerContext context);
}
```

`Serialize(null)`: [K31](01-identity-and-decisions.md#key-decisions) — nillable root with `xsi:nil`.

`topLevelValues`: `false` (default) = children of one document element (STJ array). `true` = successive XML documents in the stream (STJ JSON Lines analogue). Each document may carry its own XML declaration and documents may be separated by white space; the serializer reads each with its own `Utf8XmlReader`, so this does not rely on the reader's `AllowMultipleRoots` (which is for fragments and rejects a second declaration).

### Behavior every overload shares

All reading goes through `Utf8XmlReader` and all writing through `Utf8XmlWriter`. Every overload resolves an `XmlTypeInfo` and calls one internal read or write path; the `object` + `Type` overloads use the same path and unbox once.

| Topic | Rule |
| --- | --- |
| Unsupported type | `NotSupportedException` (`The type 'X' is not supported.`), as STJ. `TryGetTypeInfo` returns `false`. A payload problem is `XmlException`. |
| Misconfigured converter | `InvalidOperationException`: `[XmlConverter]` naming a type that is not a converter, a factory that returns `null` or another factory, a converter whose `T` is not the requested type, a resolver that answers with another type. These are never reported as "not supported". |
| Read-only | Options become read-only on first `GetTypeInfo` / `TryGetTypeInfo` or first serializer call (not on `GetConverter`). Every setter and both lists then throw `InvalidOperationException`. |
| `TypeInfoResolver` | Unset: `DefaultXmlTypeInfoResolver.Instance` when `IsReflectionEnabledByDefault`, else `null` (and using the options throws `InvalidOperationException`). An explicitly set resolver, the default one included, works with the switch off. |
| `TypeInfoResolverChain` | A view of `TypeInfoResolver`, as STJ: empty until a resolver is set, the single resolver or the items of a chain afterwards. Changing the list makes it the resolver; setting the resolver replaces the list's content. It is not a second list searched before the resolver. |
| `XmlSerializerContext` | The constructor makes the context the `TypeInfoResolver` of its options (new options when `null`), as STJ. Read-only options are rejected. |
| Root name | Must match on read: local name, and namespace when `DefaultNamespace` is set. |
| Whole-document sources | String, chars, UTF-8, sequence, stream, pipe, DOM: after the root, the rest of the document is read, so trailing junk or a second root is an `XmlException`. The `ref Utf8XmlReader` overload reads one element and leaves the reader on its end. |
| Character content | Text, CDATA, and character or entity references inside one element are joined; comments and processing instructions are ignored. `<Int32>4<!--c-->2</Int32>` is 42, as `XmlElement.GetInt32()` reads it. A child element is an `XmlException`. |
| Empty element | The empty string. Valid for `string`, `Uri`, and `byte[]`; an `XmlException` for every other built-in type (XSD has no empty `xs:int`). |
| `xsi:nil` | Handled once, by `XmlTypeInfo<T>`, before any converter (unless `HandleNull`). Nil on a non-nillable type is an `XmlException`. |
| `NumberHandling.Strict` | Writing or reading any non-finite `float` / `double` is an `XmlException`. |
| Enums | Written as the member name, or the number under `EnumHandling.Integer`; `[Flags]` as space-separated names; a value with no name as its number. Read from a name or a number in either mode. A number must fit the underlying type and, without `[Flags]`, be a declared member. |
| Stream and pipe sources | Read to the end into a pooled buffer; `MaxDocumentByteCount` stops the read as soon as it is exceeded. `DeserializeAsync` does not block on the stream. A pipe is consumed as it is read, so a bounded pipe cannot stall. |
| Failure while writing | What the writer still holds is dropped. A document that fits the writer buffer (16 KB by default) never reaches the destination half written; a longer one may have been passed on in part. |
| DOM sources | The element or node is written to a pooled buffer as XML 1.1, with the namespace declarations it inherits, and read back. Content from a 1.0 or 1.1 document is unchanged. |
| Async sequences | `SerializeAsyncEnumerable` writes each value as it arrives and flushes between values. `DeserializeAsyncEnumerable` returns each value as soon as the stream has delivered all of it: after every read it yields the values that are complete and keeps only the unfinished tail, so memory is one read plus at most one value, not the stream. A malformed part throws when it is reached; earlier values have already been returned. |

```csharp
namespace CloudNimble.Text.Xml.Serialization;

public abstract class XmlReferenceHandler
{
    public static XmlReferenceHandler Preserve { get; }
    public static XmlReferenceHandler IgnoreCycles { get; }
    public abstract XmlReferenceResolver CreateResolver();
}

public abstract class XmlReferenceResolver
{
    public abstract string GetReference(object value, out bool alreadyExists);
    public abstract void AddReference(string referenceId, object value);
    public abstract object? ResolveReference(string referenceId);
}
```

Preserve writes `id` / `ref` attributes in namespace `urn:CloudNimble.Text.Xml:refs`, wherever an object appears: the document element, a member, a collection item, an item in a wrapper. IgnoreCycles writes an empty nillable element in place of an object that is reached from inside itself, and only then: an object that repeats without a cycle is written in full each time (STJ). A caller-defined handler gets the same `id` / `ref` attributes from the identifiers its resolver returns; an empty identifier means no `id`, and nil for an object the resolver has already seen. Value types and collections are not tracked. A type created through a constructor with parameters is registered under its `id` once it has been constructed, so a later `ref` to it resolves; it cannot hold a reference to itself. Without a handler an `id` attribute is ignored and a `ref` attribute is an `XmlException`.

Feature switch:

```xml
<RuntimeHostConfigurationOption
    Include="CloudNimble.Text.Xml.XmlSerializer.IsReflectionEnabledByDefault"
    Value="true"
    Trim="true" />
```

## STJ member map

| STJ | XmlEssentials |
| --- | --- |
| Serialize/Deserialize T + options/typeInfo/context for string, utf8, span, ROS, stream, writer, IBufferWriter, async | yes, rectangular |
| object+Type + context, including DOM | yes |
| `PipeReader` / `PipeWriter` | async only, as STJ. A `PipeWriter` is an `IBufferWriter<byte>`, so the synchronous `Serialize(IBufferWriter<byte>, …)` overload takes one |
| `DeserializeAsyncEnumerable` / `SerializeAsyncEnumerable` (`topLevelValues`) | yes |
| `ReferenceHandler` | `XmlReferenceHandler` |
| `Dictionary<,>` | mapped |
| Source-gen Metadata + Serialization + interceptors | yes |
| `Default` / `Web` / `Strict` options | yes |
| `PascalCase` naming policy | yes |
| `JsonNumberHandling.WriteAsString` | omitted (K33; JSON-string numbers have no XML analogue) |

## Type info / context

```csharp
namespace CloudNimble.Text.Xml.Serialization;

public interface IXmlTypeInfoResolver
{
    XmlTypeInfo? GetTypeInfo(Type type, XmlSerializerOptions options);
}

public sealed class DefaultXmlTypeInfoResolver : IXmlTypeInfoResolver
{
    public static DefaultXmlTypeInfoResolver Instance { get; }
    public XmlTypeInfo? GetTypeInfo(Type type, XmlSerializerOptions options);
}

public abstract class XmlTypeInfo
{
    public Type Type { get; }
    public XmlConverter Converter { get; }
    public XmlSerializerOptions Options { get; }
    public XmlName RootName { get; }
    public bool IsNillable { get; }
    public Func<object>? CreateObject { get; }
    public IList<XmlPropertyInfo> Properties { get; }
    public XmlPolymorphismOptions? PolymorphismOptions { get; }
}

public abstract class XmlTypeInfo<T> : XmlTypeInfo
{
    public new XmlConverter<T> Converter { get; }
}

public sealed class XmlPropertyInfo
{
    public XmlName Name { get; }
    public bool IsAttribute { get; }
    public bool IsText { get; }
    public bool IsNillable { get; }
    public bool IsRequired { get; }
    public int Order { get; }
    public XmlTypeInfo PropertyType { get; }
}

public sealed class XmlPolymorphismOptions
{
    public XmlDiscriminatorPlacement DiscriminatorPlacement { get; }
    public string? TypeDiscriminatorPropertyName { get; }
    public IList<XmlDerivedTypeInfo> DerivedTypes { get; }
}

public readonly struct XmlDerivedTypeInfo
{
    public Type Type { get; }
    public string Discriminator { get; }
    public string? Namespace { get; }
}

public abstract class XmlSerializerContext : IXmlTypeInfoResolver
{
    protected XmlSerializerContext(XmlSerializerOptions? options);
    public XmlSerializerOptions Options { get; }
    public abstract XmlTypeInfo? GetTypeInfo(Type type);
    XmlTypeInfo? IXmlTypeInfoResolver.GetTypeInfo(Type type, XmlSerializerOptions options);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class XmlSerializableAttribute : Attribute
{
    public XmlSerializableAttribute(Type type);
    public Type Type { get; }
    public string? TypeInfoPropertyName { get; set; }
    public XmlSourceGenerationMode GenerationMode { get; set; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class XmlSourceGenerationOptionsAttribute : Attribute
{
    public bool WriteIndented { get; set; }
    public bool OmitXmlDeclaration { get; set; }
    public bool EmitByteOrderMark { get; set; }
    public XmlVersion XmlVersion { get; set; }
    public string? DefaultNamespace { get; set; }
    public bool IncludeFields { get; set; }
    public bool IgnoreReadOnlyProperties { get; set; }
    public bool IgnoreReadOnlyFields { get; set; }
    public XmlKnownNamingPolicy PropertyNamingPolicy { get; set; }
    public XmlKnownNamingPolicy ElementNamingPolicy { get; set; }
    public XmlKnownNamingPolicy AttributeNamingPolicy { get; set; }
    public XmlIgnoreCondition DefaultIgnoreCondition { get; set; }
    public XmlPropertyMapping DefaultPropertyMapping { get; set; }
    public XmlSourceGenerationMode GenerationMode { get; set; }
    public bool PropertyNameCaseInsensitive { get; set; }
    public bool RespectNullableAnnotations { get; set; }
    public bool UseStringEnumConverter { get; set; }
    public XmlNilHandling NilHandling { get; set; }
    public XmlEnumHandling EnumHandling { get; set; }
    public int MaxDepth { get; set; }
    public XmlDiscriminatorPlacement DiscriminatorPlacement { get; set; }
}
```

`XmlPropertyInfo.Order` default is **-1** (unset, [K36](01-identity-and-decisions.md#key-decisions)).

`[XmlSourceGenerationOptions]` is a **default seed**. Generated `XmlSerializerContext` ctor options win when the consumer passes `new AppXmlContext(options)`.

Generator **must** emit:

```csharp
public partial class AppXmlContext
{
    public static AppXmlContext Default { get; }
    public XmlTypeInfo<Person> Person { get; }
}
```

Usage:

```csharp
[XmlSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = XmlKnownNamingPolicy.CamelCase)]
[XmlSerializable(typeof(Person))]
[XmlSerializable(typeof(Address))]
internal partial class AppXmlContext : XmlSerializerContext { }

string xml = XmlSerializer.Serialize(person, AppXmlContext.Default.Person);
Person? p = XmlSerializer.Deserialize(xml, AppXmlContext.Default.Person);
```

Generation mode: **both** `Serialization` and `Metadata`. `Default` = `Metadata | Serialization` ([K41](01-identity-and-decisions.md#key-decisions)). Interceptors: [K42](01-identity-and-decisions.md#key-decisions).

Converter lookup and `HandleNull`: [converters](08-api-converters-and-attributes.md). Generator packing: [source generation](10-source-generation.md).

---

[Index](README.md) · [Previous: DOM](06-api-dom.md) · [Next: Converters and attributes](08-api-converters-and-attributes.md)
