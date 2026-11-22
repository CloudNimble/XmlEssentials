# Utf8XmlWriter

Part of the [XmlEssentials spec](README.md).

This page is **authoritative** for writer types. Family: implementers provide `string` / `ReadOnlySpan<char>` / `ReadOnlySpan<byte>` (utf8) and prefix+ns variants wherever a family is listed. BOM: `EmitByteOrderMark` (default `false`). Declaration text: [XML fidelity](02-xml-fidelity.md#xml-10-and-11).

## XmlEncodedText

```csharp
namespace CloudNimble.Text.Xml;

public readonly struct XmlEncodedText : IEquatable<XmlEncodedText>
{
    public static XmlEncodedText Encode(string value);
    public static XmlEncodedText Encode(ReadOnlySpan<char> value);
    public static XmlEncodedText Encode(ReadOnlySpan<byte> utf8Value);

    public ReadOnlySpan<byte> EncodedUtf8Bytes { get; }
    public override string ToString();
}
```

Caches escaped UTF-8 for hot element/attribute names (STJ `JsonEncodedText`).

## Writer options

```csharp
namespace CloudNimble.Text.Xml;

public struct XmlWriterOptions
{
    public bool Indented { get; set; }
    public char IndentCharacter { get; set; }   // default ' '
    public int IndentSize { get; set; }         // default 2
    public string NewLine { get; set; }         // default Environment.NewLine
    public int MaxDepth { get; set; }           // 0 → 64
    public int MaxAttributeCount { get; set; }  // 0 → 1000; includes xmlns*; attribute storm
    public int MaxNameByteCount { get; set; }   // 0 → 1024; element/attribute/prefix names
    public bool SkipValidation { get; set; }
    public bool OmitXmlDeclaration { get; set; }
    public bool EmitByteOrderMark { get; set; } // default false; EF BB BF at first output
    public bool? Standalone { get; set; }       // null = omit standalone= from declaration
    public XmlVersion Version { get; set; }     // default Version10
}
```

`0` on max-* means table default ([K32](01-identity-and-decisions.md#key-decisions)). Writer does **not** expose `MaxTextByteCount` / `MaxCommentByteCount` (caller-supplied payloads). `EncodeInvalidCharsAsHex` is omitted ([K34](01-identity-and-decisions.md#key-decisions)). Char legality follows `Version`. In 1.1, RestrictedChar in values is written as a numeric character reference.

## Utf8XmlWriter

```csharp
namespace CloudNimble.Text.Xml;

public sealed class Utf8XmlWriter : IDisposable, IAsyncDisposable
{
    public Utf8XmlWriter(IBufferWriter<byte> bufferWriter, XmlWriterOptions options = default);
    public Utf8XmlWriter(Stream utf8Xml, XmlWriterOptions options = default);

    public long BytesCommitted { get; }
    public int BytesPending { get; }
    public int CurrentDepth { get; }
    public XmlWriterOptions Options { get; }

    public void Dispose();
    public ValueTask DisposeAsync();
    public void Flush();
    public Task FlushAsync(CancellationToken cancellationToken = default);
    public void Reset();
    public void Reset(IBufferWriter<byte> bufferWriter);
    public void Reset(Stream utf8Xml);

    public void WriteXmlDeclaration(); // <?xml version="1.0"|"1.1" encoding="utf-8"?> [standalone]; BOM is EmitByteOrderMark at first output, not this call

    // Family: local | local+ns | prefix+local+ns × string | ReadOnlySpan<char> | ReadOnlySpan<byte> | XmlEncodedText
    public void WriteStartElement(string localName);
    public void WriteStartElement(string localName, string? namespaceUri);
    public void WriteStartElement(string? prefix, string localName, string? namespaceUri);
    public void WriteStartElement(ReadOnlySpan<char> localName);
    public void WriteStartElement(ReadOnlySpan<byte> utf8LocalName);
    public void WriteStartElement(XmlEncodedText localName);
    public void WriteStartElement(ReadOnlySpan<char> localName, ReadOnlySpan<char> namespaceUri);
    public void WriteStartElement(ReadOnlySpan<byte> utf8LocalName, ReadOnlySpan<byte> utf8NamespaceUri);
    public void WriteStartElement(ReadOnlySpan<byte> utf8Prefix, ReadOnlySpan<byte> utf8LocalName, ReadOnlySpan<byte> utf8NamespaceUri);

    public void WriteEndElement();
    public void WriteFullEndElement();

    public void WriteElementString(string localName, string? value);
    public void WriteElementString(string localName, string? namespaceUri, string? value);
    public void WriteElementString(string localName, DateTime value);
    public void WriteElementString(string localName, DateTimeOffset value);
    public void WriteElementString(string localName, Guid value);
    public void WriteElementString(ReadOnlySpan<char> localName, ReadOnlySpan<char> value);
    public void WriteElementString(ReadOnlySpan<byte> utf8LocalName, ReadOnlySpan<byte> utf8Value);

    public void WriteElementNumber(string localName, int value);
    public void WriteElementNumber(string localName, long value);
    public void WriteElementNumber(string localName, double value);
    public void WriteElementNumber(string localName, float value);
    public void WriteElementNumber(string localName, decimal value);
    [CLSCompliant(false)] public void WriteElementNumber(string localName, uint value);
    [CLSCompliant(false)] public void WriteElementNumber(string localName, ulong value);

    public void WriteElementBoolean(string localName, bool value);
    public void WriteElementBoolean(ReadOnlySpan<char> localName, bool value);
    public void WriteElementBoolean(ReadOnlySpan<byte> utf8LocalName, bool value);
    public void WriteElementBase64String(string localName, ReadOnlySpan<byte> bytes);
    public void WriteElementBase64String(ReadOnlySpan<char> localName, ReadOnlySpan<byte> bytes);
    public void WriteElementBase64String(ReadOnlySpan<byte> utf8LocalName, ReadOnlySpan<byte> bytes);

    public void WriteNilElement(string localName);
    public void WriteNilElement(string localName, string? namespaceUri);
    public void WriteNilElement(ReadOnlySpan<char> localName);
    public void WriteNilElement(ReadOnlySpan<byte> utf8LocalName);

    public void WriteAttributeString(string name, string? value);
    public void WriteAttributeString(string localName, string? namespaceUri, string? value);
    public void WriteAttributeString(string? prefix, string localName, string? namespaceUri, string? value);
    public void WriteAttributeString(ReadOnlySpan<byte> utf8Name, ReadOnlySpan<byte> utf8Value);
    public void WriteAttributeString(ReadOnlySpan<char> name, ReadOnlySpan<char> value);

    public void WriteStartAttribute(string name);
    public void WriteStartAttribute(ReadOnlySpan<char> name);
    public void WriteStartAttribute(string? prefix, string localName, string? namespaceUri);
    public void WriteEndAttribute();

    public void WriteNamespaceDeclaration(string prefix, string namespaceUri);
    public void WriteNamespaceDeclaration(ReadOnlySpan<byte> utf8Prefix, ReadOnlySpan<byte> utf8NamespaceUri);
    public void WriteDefaultNamespace(string namespaceUri);
    public void WriteDefaultNamespace(ReadOnlySpan<byte> utf8NamespaceUri);

    public void WriteStringValue(string? value);
    public void WriteStringValue(ReadOnlySpan<char> value);
    public void WriteStringValue(ReadOnlySpan<byte> utf8Value);
    public void WriteStringValue(DateTime value);
    public void WriteStringValue(DateTimeOffset value);
    public void WriteStringValue(DateOnly value);
    public void WriteStringValue(TimeOnly value);
    public void WriteStringValue(TimeSpan value);
    public void WriteStringValue(Guid value);

    public void WriteNumberValue(int value);
    public void WriteNumberValue(long value);
    public void WriteNumberValue(double value);
    public void WriteNumberValue(float value);
    public void WriteNumberValue(decimal value);
    [CLSCompliant(false)] public void WriteNumberValue(uint value);
    [CLSCompliant(false)] public void WriteNumberValue(ulong value);

    public void WriteBooleanValue(bool value);
    public void WriteBase64StringValue(ReadOnlySpan<byte> bytes);
    public void WriteNilValue();

    public void WriteStringValueSegment(ReadOnlySpan<byte> value, bool isFinalSegment);
    public void WriteStringValueSegment(ReadOnlySpan<char> value, bool isFinalSegment);
    public void WriteBase64StringSegment(ReadOnlySpan<byte> value, bool isFinalSegment);

    public void WriteCData(string text);
    public void WriteCData(ReadOnlySpan<char> text);
    public void WriteCData(ReadOnlySpan<byte> utf8Value);
    public void WriteComment(string value);
    public void WriteComment(ReadOnlySpan<char> value);
    public void WriteComment(ReadOnlySpan<byte> utf8Value);
    public void WriteProcessingInstruction(string target, string? data);
    public void WriteRawValue(ReadOnlySpan<byte> utf8Xml, bool skipInputValidation = false);
    public void WriteRawValue(ReadOnlySpan<char> xml, bool skipInputValidation = false);
    public void WriteRawValue(string xml, bool skipInputValidation = false);
}
```

Namespace declarations: if the caller writes an element in a URI that is not in scope, the writer **auto-declares** a prefix (`ns1`, `ns2`, …) or a default xmlns when `prefix` is null/empty. Explicit `WriteNamespaceDeclaration` / `WriteDefaultNamespace` must occur on the start tag (before attributes that use them, and before content).

`Dispose` flushes. If `SkipValidation` is false and elements are unclosed, `Dispose` throws `XmlException`.

`WriteNilValue` after attributes: legal. After content: `XmlException`. Other attributes plus `xsi:nil` on deserialize: attributes are applied; the nillable **element value** is null.

## Writer escaping

| Context | Escape |
| --- | --- |
| Element text | `&` → `&amp;`, `<` → `&lt;`, `]]>` → `]]&gt;` (via escaping `>` after `]]`) |
| Attribute value (always double-quoted) | `&` → `&amp;`, `<` → `&lt;`, `"` → `&quot;` |
| CDATA | no escaping; `]]>` in input throws |
| Comment | `--` in input throws |
| Names | no escaping; invalid `Name` throws |
| `>` in text | escaped as `&gt;` only when required (`]]>` case); otherwise may be literal `>` |

Illegal characters in values throw. In 1.1, RestrictedChar is emitted as a numeric character reference, not as a literal.

Typed write lexical forms: [XML fidelity](02-xml-fidelity.md#lexical-rules-xsd-10-invariant).

---

[Index](README.md) · [Previous: Utf8XmlReader](04-api-reader.md) · [Next: DOM](06-api-dom.md)
