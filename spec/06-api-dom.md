# DOM

Part of the [XmlEssentials spec](README.md).

## XmlValueKind

```csharp
namespace CloudNimble.Text.Xml;

public enum XmlValueKind : byte
{
    Undefined = 0,
    Document = 1,
    Element = 2,
    EmptyElement = 3,
    Attribute = 4,
    Text = 5,
    CData = 6,
    Comment = 7,
    ProcessingInstruction = 8,
    XmlDeclaration = 9,
    Nil = 10,
}
```

## DOM — immutable (JsonDocument / JsonElement)

Name collision with `System.Xml.XmlDocument` / `XmlElement` is accepted.

**Pooled** means the parse buffer is rented from `ArrayPool<byte>` (STJ `JsonDocument`). `Dispose` returns the rental. Use after dispose throws `ObjectDisposedException`. There is **no** document-instance cache.

```csharp
namespace CloudNimble.Text.Xml;

public readonly struct XmlDeclarationInfo
{
    public string Version { get; }    // "1.0"
    public string Encoding { get; }   // "utf-8"
    public bool? Standalone { get; }  // null = omitted
}

public struct XmlDocumentOptions
{
    public XmlCommentHandling CommentHandling { get; set; }         // default Allow, the zero value: a parsed document keeps its comments so that WriteTo round-trips them. Set Skip to leave them out.
    public XmlWhitespaceHandling WhitespaceHandling { get; set; }   // default Significant, the zero value: indentation between elements is kept so that WriteTo does not change the layout. Set Insignificant to leave it out.
    public int MaxDepth { get; set; }
    public int MaxAttributeCount { get; set; }
    public int MaxNameByteCount { get; set; }
    public int MaxCommentByteCount { get; set; }
    public int MaxTextByteCount { get; set; }
    public int MaxDocumentByteCount { get; set; }
}

public sealed class XmlDocument : IDisposable
{
    public static XmlDocument Parse(string xml, XmlDocumentOptions options = default);
    public static XmlDocument Parse(ReadOnlySpan<char> xml, XmlDocumentOptions options = default);
    public static XmlDocument Parse(ReadOnlySpan<byte> utf8Xml, XmlDocumentOptions options = default);
    public static XmlDocument Parse(ReadOnlySequence<byte> utf8Xml, XmlDocumentOptions options = default);
    public static XmlDocument Parse(Stream utf8Xml, XmlDocumentOptions options = default);
    public static XmlDocument ParseValue(ref Utf8XmlReader reader);

    public static bool TryParse(ReadOnlySpan<byte> utf8Xml, XmlDocumentOptions options, out XmlDocument? document);

    public XmlElement RootElement { get; }
    /// <summary>Immutable view. Not <see cref="Nodes.XmlDeclarationNode"/> (mutable tree only).</summary>
    public XmlDeclarationInfo? Declaration { get; }
    public void WriteTo(Utf8XmlWriter writer);
    public void Dispose();
}

public readonly struct XmlElement : IEquatable<XmlElement>
{
    public XmlValueKind ValueKind { get; }
    public XmlTokenType TokenType { get; }
    public string LocalName { get; }
    public string NamespaceUri { get; }
    public string Prefix { get; }
    public bool IsEmptyElement { get; }
    public bool IsNil { get; }
    public int AttributeCount { get; }

    public XmlElement GetChild(string localName);
    public XmlElement GetChild(string localName, string namespaceUri);
    public bool TryGetChild(string localName, out XmlElement value);
    public bool TryGetChild(string localName, string namespaceUri, out XmlElement value);

    public XmlElement GetAttribute(string localName);
    public XmlElement GetAttribute(string localName, string namespaceUri);
    public bool TryGetAttribute(string localName, out XmlElement value);

    public string GetRawXml();
    public string GetInnerText();
    public string? GetString();
    public bool GetBoolean();
    public int GetInt32();
    public long GetInt64();
    public double GetDouble();
    public decimal GetDecimal();
    public DateTime GetDateTime();
    public DateTimeOffset GetDateTimeOffset();
    public Guid GetGuid();
    public byte[] GetBytesFromBase64();

    public Enumerator EnumerateChildren();
    public AttributeEnumerator EnumerateAttributes();
    public void WriteTo(Utf8XmlWriter writer);
    public XmlNode CloneNode();

    public struct Enumerator
    {
        public XmlElement Current { get; }
        public bool MoveNext();
        public Enumerator GetEnumerator();
    }

    public struct AttributeEnumerator
    {
        public XmlElement Current { get; }
        public bool MoveNext();
        public AttributeEnumerator GetEnumerator();
    }
}
```

`XmlElement` is a **struct cursor** into `XmlDocument` (STJ `JsonElement`). Enumerators are **nested** (`XmlElement.Enumerator`), matching STJ `JsonElement.ArrayEnumerator` — not namespace-level types named `Enumerator`.

`Declaration` is `XmlDeclarationInfo?` (readonly struct). Mutating `Nodes.XmlDeclarationNode` must not be possible through `XmlDocument`. `WriteTo` uses the infoset captured at parse.

Prolog/epilog comments and PIs are **preserved** in the parse buffer and round-tripped by `XmlDocument.WriteTo`. They are **not** enumerable as children of `RootElement`.

### Values

- `GetString()` returns the text exactly as parsed (after the reader's line-end and attribute-value normalization), whitespace included. For an element it is the concatenated text and CDATA of its descendants. For a nil element it is `null`.
- `GetInt32()`, `GetDouble()`, `GetDateTime()`, and the other typed getters apply the lexical rules in [XML fidelity](02-xml-fidelity.md#lexical-rules-xsd-10-invariant) through the same code as `Utf8XmlReader`. `<n> 42 </n>` is the string `" 42 "` and the integer `42`.
- A typed getter on a nil element throws: the element has no value.
- `IsEmptyElement` is true for `<e/>` and false for `<e></e>`, including when the element is nil.
- Name lookups compare `{local, namespace URI}`. The overloads without a namespace match the local name in any namespace.

### WriteTo fidelity

`WriteTo` writes the infoset that was parsed. The exact guarantees are listed under [round-trip guarantees](02-xml-fidelity.md#round-trip-guarantees): namespace declarations as parsed and in place, `xmlns=""` kept (and added for a sub-tree that needs it), attribute order kept, `<e/>` versus `<e></e>` kept. A document that is already in the writer's own form is reproduced byte for byte.

Writing a sub-tree (`XmlElement.WriteTo`, `GetRawXml`, `ParseValue`) declares any namespace its own name or attributes need that an ancestor declared.

The writer's `XmlWriterOptions.Version` governs what can be written. A 1.1 document that undeclares a prefix (`xmlns:p=""`) or holds a RestrictedChar cannot be written by a 1.0 writer and throws.

### Storage

Rows are stored in document order (STJ `JsonDocument.MetadataDb`): an element, its attributes, then its descendants. The document owns one pooled copy of the UTF-8 payload; names and unescaped values are offsets into it. Details and measurements: [optimization](23-optimization.md).

## DOM — mutable (JsonNode family)

XML is not a property bag. The mutable tree is a real XML node list, not `JsonObject`.

```csharp
namespace CloudNimble.Text.Xml.Nodes;

public abstract class XmlNode
{
    public abstract XmlValueKind ValueKind { get; }
    public XmlNode? Parent { get; }
    public abstract string? NamespaceUri { get; }

    public static XmlNode? Parse(string xml); // document → root XmlElementNode; prolog comments/PIs discarded
    public static XmlNode? Parse(ReadOnlySpan<byte> utf8Xml);
    public static XmlNode? ParseValue(ref Utf8XmlReader reader);

    public abstract void WriteTo(Utf8XmlWriter writer);
    public override string ToString();
    public XmlNode DeepClone();
}

public sealed class XmlNodeList : IList<XmlNode>
{
    public int Count { get; }
    public XmlNode this[int index] { get; set; }
    public void Add(XmlNode node);
    public void Insert(int index, XmlNode node);
    public bool Remove(XmlNode node);
    public void RemoveAt(int index);
    public void Clear();
    public IEnumerator<XmlNode> GetEnumerator();
}

public sealed class XmlAttributeNodeList : IList<XmlAttributeNode>
{
    public int Count { get; }
    public XmlAttributeNode this[int index] { get; set; }
    public XmlAttributeNode? this[string localName] { get; }
    public XmlAttributeNode? this[string localName, string namespaceUri] { get; }
    public void Add(XmlAttributeNode node);
    public bool Remove(XmlAttributeNode node);
    public void Clear();
    public IEnumerator<XmlAttributeNode> GetEnumerator();
}

public sealed class XmlElementNode : XmlNode, IEnumerable<XmlNode>
{
    public XmlElementNode(string localName, string? namespaceUri = null, string? prefix = null);

    public string LocalName { get; set; }
    public string Prefix { get; set; }
    public override string NamespaceUri { get; set; }

    public XmlAttributeNodeList Attributes { get; }
    public XmlNodeList Children { get; }
    public string InnerText { get; set; }

    public XmlElementNode? this[string localName] { get; }
    public XmlElementNode? this[string localName, string namespaceUri] { get; }

    public XmlElementNode AddElement(string localName, string? namespaceUri = null);
    public XmlAttributeNode SetAttribute(string localName, string? value, string? namespaceUri = null);
    public void Add(XmlNode node);
}

public sealed class XmlAttributeNode : XmlNode
{
    public string LocalName { get; set; }
    public string Prefix { get; set; }
    public override string NamespaceUri { get; set; }
    public string Value { get; set; }
}

public sealed class XmlTextNode : XmlNode { public string Value { get; set; } }
public sealed class XmlCDataNode : XmlNode { public string Value { get; set; } }
public sealed class XmlCommentNode : XmlNode { public string Value { get; set; } }
public sealed class XmlProcessingInstructionNode : XmlNode
{
    public string Target { get; set; }
    public string? Data { get; set; }
}

/// <summary>Mutable tree only. Immutable documents use <see cref="XmlDeclarationInfo"/>.</summary>
public sealed class XmlDeclarationNode : XmlNode
{
    public string Version { get; set; }
    public string Encoding { get; set; }
    public bool? Standalone { get; set; }
}
```

`XmlNodeList` / `XmlAttributeNodeList` are our types in `CloudNimble.Text.Xml.Nodes`, not BCL.

Serializer integration: a property of type `XmlNode` / `XmlElementNode` is an XML island.

---

[Index](README.md) · [Previous: Utf8XmlWriter](05-api-writer.md) · [Next: XmlSerializer](07-api-serializer.md)
