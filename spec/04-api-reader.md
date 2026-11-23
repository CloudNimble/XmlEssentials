# Utf8XmlReader

Part of the [XmlEssentials spec](README.md).

Public API pages are [04](04-api-reader.md)–[08](08-api-converters-and-attributes.md). Namespace default is `CloudNimble.Text.Xml` unless noted. Attributes and some enums live in `CloudNimble.Text.Xml.Serialization`.

**Each of 04–08 is authoritative for the types it defines.** Overloads that only change `string` / `ReadOnlySpan<char>` / `ReadOnlySpan<byte>` (utf8) are listed as a family on that page; implementers **must** provide the trio where the family comment says so, plus prefix+ns variants where XML requires them. [07](07-api-serializer.md) is authoritative for `XmlSerializer` overloads (including a STJ mapping table).

## Enums

```csharp
namespace CloudNimble.Text.Xml;

public enum XmlTokenType : byte
{
    None = 0,
    StartElement = 1,
    EndElement = 2,
    Attribute = 3,
    Text = 4,
    CData = 5,
    Comment = 6,
    ProcessingInstruction = 7,
    XmlDeclaration = 8,
}

public enum XmlCommentHandling : byte
{
    Allow = 0,    // default (the zero value): comments are reported, so XML that is read and written back keeps them
    Skip = 1,
    Disallow = 2,
}

public enum XmlWhitespaceHandling : byte
{
    Significant = 0,   // default (the zero value): whitespace-only text between elements is reported (XML 1.0 section 2.10)
    Insignificant = 1, // skip whitespace-only text between elements
    None = 2,
}

public enum XmlVersion : byte
{
    Version10 = 0,
    Version11 = 1,
}
```

`XmlVersion` is also described in [XML fidelity](02-xml-fidelity.md#xml-10-and-11). This page is authoritative for the enum.

`XmlValueKind` is on the [DOM page](06-api-dom.md). Serializer enums are on the [serializer page](07-api-serializer.md#enums).

## Constants

```csharp
namespace CloudNimble.Text.Xml;

public static class XmlNamespaces
{
    public const string Xml = "http://www.w3.org/XML/1998/namespace";
    public const string Xmlns = "http://www.w3.org/2000/xmlns/";
    public const string Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    public const string Xsd = "http://www.w3.org/2001/XMLSchema";
}

public static class XmlAttributes
{
    public const string Nil = "nil";           // local name in Xsi ns
    public const string Type = "type";         // xsi:type local name
}
```

## XmlName

Our qualified name. **Not** `System.Xml.XmlQualifiedName`.

```csharp
namespace CloudNimble.Text.Xml;

public readonly struct XmlName : IEquatable<XmlName>
{
    public XmlName(string localName, string? namespaceUri = null, string? prefix = null);

    public string LocalName { get; }
    public string NamespaceUri { get; } // "" if none
    public string Prefix { get; }       // "" if none; NOT part of equality

    public bool Equals(XmlName other);  // LocalName + NamespaceUri, ordinal
    public override int GetHashCode();
    public override string ToString();  // "{ns}local" or "prefix:local" if prefix set

    public static bool operator ==(XmlName left, XmlName right);
    public static bool operator !=(XmlName left, XmlName right);
}
```

## XmlException

```csharp
namespace CloudNimble.Text.Xml;

public sealed class XmlException : System.Exception
{
    public XmlException(string message);
    public XmlException(string message, Exception? innerException);
    public XmlException(string message, long? bytePosition, long? lineNumber, long? linePosition, string? path);

    public long? BytePosition { get; }
    public long? LineNumber { get; }     // 1-based after EOL normalization
    public long? LinePosition { get; }   // 1-based, UTF-8 bytes in the current line (STJ-like, not xmlconf columns)
    public string? Path { get; }         // /Person/Address/City or /Person/@id
}
```

Do not use or wrap `System.Xml.XmlException`.

## Reader options / state

```csharp
namespace CloudNimble.Text.Xml;

public struct XmlReaderOptions
{
    public XmlCommentHandling CommentHandling { get; set; }         // default Allow (the zero value); default and new() are the same
    public XmlWhitespaceHandling WhitespaceHandling { get; set; }   // default Significant (the zero value)
    public int MaxDepth { get; set; }                               // 0 → 64
    public int MaxAttributeCount { get; set; }                      // 0 → 1000; includes xmlns*
    public int MaxNameByteCount { get; set; }                       // 0 → 1024
    public int MaxCommentByteCount { get; set; }                    // 0 → 1_048_576
    public int MaxTextByteCount { get; set; }                       // 0 → 16_777_216
    public int MaxDocumentByteCount { get; set; }                   // 0 = unbounded
    public bool AllowMultipleRoots { get; set; }                    // default false
}

public readonly struct XmlReaderState
{
    public XmlReaderState(XmlReaderOptions options = default);
    public XmlReaderOptions Options { get; }
}
```

Public surface matches `JsonReaderState`: **only `Options` is visible.** Limit `0` semantics: [DoS limits](02-xml-fidelity.md#dos-limits-defaults).

### XmlReaderState implementation contract

XML cannot resume on STJ’s `BitStack`. `Utf8XmlReader` is a `ref struct`; resume **requires** heap (or a pooled buffer **owned by the state**) for data that outlives the previous `ReadOnlySpan<byte>`.

`CurrentState` **must** return a state that snapshots **all** of:

1. **Element stack** — per depth: local name + namespace URI (copied strings or interned UTF-8, **not** offsets into the previous buffer) so end tags can be matched ([well-formedness rule 2](02-xml-fidelity.md#well-formedness-constraints-the-reader-enforces)).
2. **Namespace scopes** — the stack under [Namespace stack](#namespace-stack). Prefix and URI bytes/strings are copied or interned. `xmlns=""` unbinding is part of the snapshot. `xmlns:p=""` (1.1 only) is part of the snapshot.
3. **`XmlVersion`** — so a resume after the declaration keeps 1.1 Char/Name/EOL rules.
4. **Partial token** (incomplete name/attr/text/CDATA/comment/PI at `IsFinalBlock=false`), attribute cursor, `TokenType`, `IsEmptyElement`.
5. **Depth**, `BytesConsumed` (relative to the current buffer — documented as “consumed in this block”; absolute document position is `TokenStartIndex` / line counters).
6. **Line/byte position** after EOL normalization.

**Copy semantics:** `CurrentState` is copy-on-read: the returned struct holds an **immutable snapshot** (or a COW clone). Two `Utf8XmlReader` instances constructed from the same or copied `XmlReaderState` **must not** share a mutable scope list. A naive `List<(string prefix, string uri)>` stored by reference **aliases** and is forbidden.

This is the XML tax vs JSON: `JsonReaderState` is small and often stack-ish; this one is **not** allocation-free. Callers of `CurrentState` in a hot resume loop should expect allocations or pool rentals proportional to depth × xmlns declarations.

Internal fields are not public. Tests must resume across a buffer split **inside** a namespaced start tag, an attribute value, and element text, and then match the end tag.

## Utf8XmlReader

`ref struct`. Analogous to `Utf8JsonReader`. Empty elements: `TokenType == StartElement` and `IsEmptyElement == true`. **No synthesized `EndElement` token.** Callers must check `IsEmptyElement`. `Skip()` consumes the whole element including children.

`Version` is set when the XML declaration is read (or remains `Version10` if there is none). Char / Name / EOL / `xmlns:prefix=""` use that flag for the rest of the document. The caller does not pass a version discriminator. Snapshot `CurrentState` **must** include `Version` ([implementation contract](#xmlreaderstate-implementation-contract)).

```csharp
namespace CloudNimble.Text.Xml;

public ref struct Utf8XmlReader
{
    public Utf8XmlReader(ReadOnlySpan<byte> utf8Xml, XmlReaderOptions options = default);
    public Utf8XmlReader(ReadOnlySpan<byte> utf8Xml, bool isFinalBlock, XmlReaderState state);
    public Utf8XmlReader(ReadOnlySpan<byte> utf8Xml, Span<byte> scratch, XmlReaderOptions options);
    public Utf8XmlReader(ReadOnlySpan<byte> utf8Xml, Span<byte> scratch, bool isFinalBlock, XmlReaderState state);
    public Utf8XmlReader(ReadOnlySequence<byte> utf8Xml, XmlReaderOptions options = default);
    public Utf8XmlReader(ReadOnlySequence<byte> utf8Xml, bool isFinalBlock, XmlReaderState state);

    public readonly long BytesConsumed { get; }
    public readonly int CurrentDepth { get; }
    public readonly XmlReaderState CurrentState { get; }
    public readonly bool HasValueSequence { get; }
    public readonly bool IsFinalBlock { get; }
    public readonly SequencePosition Position { get; }
    public readonly long TokenStartIndex { get; }
    public readonly XmlTokenType TokenType { get; }
    public readonly XmlVersion Version { get; }   // after declaration; Version10 if omitted
    public readonly bool ValueIsEscaped { get; }
    public readonly ReadOnlySequence<byte> ValueSequence { get; }
    public readonly ReadOnlySpan<byte> ValueSpan { get; }

    public readonly bool IsEmptyElement { get; }
    public readonly int AttributeCount { get; }
    public readonly bool HasAttributes { get; }

    public readonly ReadOnlySpan<byte> LocalNameSpan { get; }
    public readonly ReadOnlySpan<byte> NamespaceUriSpan { get; }
    public readonly ReadOnlySpan<byte> PrefixSpan { get; }

    public bool Read();
    public void Skip();
    public bool TrySkip();

    public bool MoveToNextAttribute();
    public void MoveToElement();

    public readonly int CopyString(Span<byte> utf8Destination);
    public readonly int CopyString(Span<char> destination);

    public bool GetBoolean();
    public byte GetByte();
    public byte[] GetBytesFromBase64();
    public DateTime GetDateTime();
    public DateTimeOffset GetDateTimeOffset();
    public DateOnly GetDateOnly();
    public TimeOnly GetTimeOnly();
    public TimeSpan GetTimeSpan();
    public decimal GetDecimal();
    public double GetDouble();
    public Guid GetGuid();
    public short GetInt16();
    public int GetInt32();
    public long GetInt64();
    [CLSCompliant(false)] public sbyte GetSByte();
    public float GetSingle();
    public string? GetString();
    [CLSCompliant(false)] public ushort GetUInt16();
    [CLSCompliant(false)] public uint GetUInt32();
    [CLSCompliant(false)] public ulong GetUInt64();
    public string GetComment();
    public bool GetNil(); // true if current StartElement has xsi:nil="true" (other attributes may also be present)

    public bool TryGetByte(out byte value);
    public bool TryGetBytesFromBase64([NotNullWhen(true)] out byte[]? value);
    public bool TryGetDateTime(out DateTime value);
    public bool TryGetDateTimeOffset(out DateTimeOffset value);
    public bool TryGetDateOnly(out DateOnly value);
    public bool TryGetTimeOnly(out TimeOnly value);
    public bool TryGetTimeSpan(out TimeSpan value);
    public bool TryGetDecimal(out decimal value);
    public bool TryGetDouble(out double value);
    public bool TryGetGuid(out Guid value);
    public bool TryGetInt16(out short value);
    public bool TryGetInt32(out int value);
    public bool TryGetInt64(out long value);
    [CLSCompliant(false)] public bool TryGetSByte(out sbyte value);
    public bool TryGetSingle(out float value);
    [CLSCompliant(false)] public bool TryGetUInt16(out ushort value);
    [CLSCompliant(false)] public bool TryGetUInt32(out uint value);
    [CLSCompliant(false)] public bool TryGetUInt64(out ulong value);

    public readonly bool ValueTextEquals(ReadOnlySpan<byte> utf8Text);
    public readonly bool ValueTextEquals(ReadOnlySpan<char> text);
    public readonly bool ValueTextEquals(string? text);

    public readonly bool LocalNameEquals(ReadOnlySpan<byte> utf8LocalName);
    public readonly bool LocalNameEquals(ReadOnlySpan<char> localName);
    public readonly bool LocalNameEquals(string? localName);
    public readonly bool NamespaceUriEquals(ReadOnlySpan<byte> utf8NamespaceUri);
    public readonly bool NamespaceUriEquals(ReadOnlySpan<char> namespaceUri);
    public readonly bool NamespaceUriEquals(string? namespaceUri);
}
```

Lexical rules for getters: [XML fidelity](02-xml-fidelity.md#lexical-rules-xsd-10-invariant).

## Scratch buffer

A reader needs memory of its own in four cases:

- a value that holds a reference, or a line end other than LF, is decoded into a buffer;
- a start tag has more than eight attributes;
- more than eight namespace declarations are in scope;
- elements are nested more than eight deep.

The constructors without `scratch` allocate an array for each, once per reader. The two with `scratch` take the memory from the caller's buffer and allocate only when it runs out, so any size is correct and a larger one saves more. The four real documents in the benchmarks need between 80 bytes and about 7 KB.

- The buffer's contents on entry do not matter, and it can be passed again to the reader for the next block of the same document.
- Copies of a reader share it. See "Lifetime of a decoded `ValueSpan`, and reader copies" in [open questions](20-open-questions.md).
- A buffer from `stackalloc` is fastest when it is allocated in the frame **above** the one that constructs the reader. In the same frame the reader was 18 ns slower to construct when measured.
- `XmlDocument.Parse` uses the unused end of the buffer it rents for the document, and clears what the reader wrote there. `XmlSerializer.Deserialize` from memory gives the reader 512 bytes of stack.
- There are no `ReadOnlySequence<byte>` overloads with `scratch`: a sequence of several segments is copied into an array before it is read.

## Namespace stack

The reader maintains a stack of scopes, one per in-progress element. Each scope is a list of `(prefix, namespaceUri)` declarations from that element's `xmlns` attributes, evaluated **before** the element's own name is resolved (XML NS spec: xmlns attributes on an element apply to that element).

Lookups:

- `PrefixSpan` + local name → `NamespaceUriSpan`.
- Default xmlns is prefix `""`.
- `xmlns=""` sets default URI to `""` (unbound) for this element and descendants until rebound.
- Unprefixed attributes are in **no namespace** (XML NS spec), not the default xmlns. Unprefixed elements **are** in the default xmlns. This is a frequent mapper bug; tests must cover it.

On resume, this stack is the snapshot inside `XmlReaderState`, not offsets into the previous buffer.

Resumable `IsFinalBlock` behavior is also in [architecture](03-architecture.md#async-and-streaming).

---

[Index](README.md) · [Previous: Architecture](03-architecture.md) · [Next: Utf8XmlWriter](05-api-writer.md)
