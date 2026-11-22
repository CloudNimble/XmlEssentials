# Proposed design

Part of the [XmlEssentials spec](README.md).

## Layer cake

```mermaid
flowchart TB
  subgraph app [Application]
    POCOs["POCOs / records"]
    Context["XmlSerializerContext source-gen"]
    Conv["XmlConverter&lt;T&gt;"]
  end

  subgraph high [High-level]
    Ser["XmlSerializer static"]
    Opt["XmlSerializerOptions"]
    TI["XmlTypeInfo&lt;T&gt;"]
  end

  subgraph dom [DOM]
    Doc["XmlDocument / XmlElement immutable"]
    Node["XmlNode mutable tree"]
  end

  subgraph low [UTF-8 pipeline — zero System.Xml]
    R["Utf8XmlReader ref struct"]
    W["Utf8XmlWriter"]
  end

  subgraph bytes [Bytes]
    Span["ReadOnlySpan&lt;byte&gt;"]
    Seq["ReadOnlySequence&lt;byte&gt;"]
    Stream["Stream"]
    IBW["IBufferWriter&lt;byte&gt;"]
  end

  POCOs --> Ser
  Context --> TI
  Conv --> Opt
  Ser --> TI
  Ser --> Opt
  Ser --> Doc
  Ser --> Node
  Ser --> R
  Ser --> W
  Doc --> R
  Doc --> W
  Node --> W
  R --> Span
  R --> Seq
  W --> Stream
  W --> IBW
```

There is **no** arrow to `System.Xml.*`.

## Serialize sequence

```mermaid
sequenceDiagram
  participant App
  participant XmlSerializer
  participant TypeInfo as XmlTypeInfo of T
  participant Conv as XmlConverter
  participant W as Utf8XmlWriter
  participant Dest as Stream / IBufferWriter / string

  App->>XmlSerializer: Serialize(value, options or typeInfo)
  XmlSerializer->>TypeInfo: resolve (context or reflection)
  alt OmitXmlDeclaration = false
    XmlSerializer->>W: WriteXmlDeclaration()
  end
  XmlSerializer->>W: WriteStartElement(rootName, defaultNs)
  XmlSerializer->>Conv: Write(writer, value, options)
  Conv->>W: attributes then child elements
  XmlSerializer->>W: WriteEndElement()
  XmlSerializer->>W: Flush()
  W->>Dest: UTF-8 bytes
```

## Deserialize sequence

```mermaid
sequenceDiagram
  participant App
  participant XmlSerializer
  participant R as Utf8XmlReader
  participant TypeInfo as XmlTypeInfo of T
  participant Conv as XmlConverter

  App->>XmlSerializer: Deserialize of T (utf8 / stream / reader)
  alt Stream
    XmlSerializer->>XmlSerializer: buffer into ArrayBufferWriter enforcing MaxDocumentByteCount while growing
  end
  XmlSerializer->>R: new Utf8XmlReader(span, options)
  XmlSerializer->>R: Read to root StartElement
  XmlSerializer->>TypeInfo: resolve
  XmlSerializer->>Conv: Read(ref reader, type, options)
  Conv->>R: attributes then child elements
  Conv-->>App: T
```

## Component responsibilities

| Component | Responsibility | Non-responsibility |
| --- | --- | --- |
| `Utf8XmlReader` | Well-formed UTF-8 XML 1.0/1.1 tokenize; version flag; namespace stack; resumable `IsFinalBlock` | Object mapping, runtime XSD validation, DTD |
| `Utf8XmlWriter` | Well-formed UTF-8 emit; escaping; indent; `xsi:nil`; xmlns | Mapping |
| `XmlDocument`/`XmlElement` | ArrayPool-rented immutable parse tree (STJ `JsonDocument`/`JsonElement`) | Mutation; document-instance cache |
| `XmlNode` tree | Mutable DOM with namespaces and mixed children | Mapping |
| `XmlSerializer` | POCO graph ↔ XML via `XmlTypeInfo` | Parsing (delegates to reader) |
| Source generator | `XmlTypeInfo<T>` + serialize/deserialize implementations | Runtime reflection |

DoS limits: [XML fidelity](02-xml-fidelity.md#dos-limits-defaults). Null/root: [object mapping](09-object-mapping.md). Namespace stack + **state snapshot**: [Utf8XmlReader](04-api-reader.md#xmlreaderstate-implementation-contract). Writer escaping: [Utf8XmlWriter](05-api-writer.md#writer-escaping).

## Async and streaming

- **Reader:** resumable. `IsFinalBlock=false` + incomplete token → `Read()` returns false; caller saves `CurrentState` and feeds the next buffer. `CurrentState` is an **opaque snapshot** (element stack, xmlns scopes as copied strings, partial token, positions) — not `Options` alone. Token-spanning `ReadOnlySequence` uses `HasValueSequence` / `ValueSequence`.
- **Serializer async from `Stream`:** object graph = buffer-then-parse (STJ `DeserializeAsync`). **Checking `MaxDocumentByteCount` on each grow.**
- **Serializer async to `Stream`:** `Utf8XmlWriter` on the stream, `FlushAsync`.
- **`IAsyncEnumerable`:** `DeserializeAsyncEnumerable` / `SerializeAsyncEnumerable` (STJ net11, including `topLevelValues`). XML: `false` = children of one document element; `true` = successive documents in the stream.
- **`PipeReader` / `PipeWriter`:** same shapes as STJ net11.

---

[Index](README.md) · [Previous: XML fidelity](02-xml-fidelity.md) · [Next: Utf8XmlReader](04-api-reader.md)
