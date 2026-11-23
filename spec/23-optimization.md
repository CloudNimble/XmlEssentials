# Optimization

Part of the [XmlEssentials spec](README.md).

This page is the playbook for making `Utf8XmlReader` / `Utf8XmlWriter` as fast as STJ, and for beating BCL `XmlReader` / `XmlWriter` on the same payloads. It records **how** this repo has been optimizing, **what already shipped** in PR 2–3, and **what the next pass should do**.

Read [benchmarks](15-benchmarks.md) for serializer-column rules. Reader/writer benches live in `src/CloudNimble.XmlEssentials.Performance/` and are the source of truth for tokenizer numbers.

## Constraints that survive every pass

- Zero `System.Xml*` in runtime and tests.
- Official [dotnet/runtime](https://github.com/dotnet/runtime) source at a tagged release (`v11.0.0` while TFMs are net10/net11). **Not** reflection, ILSpy, or decompilation.
- STJ toolbox: spans, `SearchValues`, `Utf8Formatter` / `IUtf8SpanFormattable`, `Ascii.FromUtf16`, `ArrayPool`, `IBufferWriter.GetMemory` / `Advance`. No string concat on the hot path.
- CloudNimble C# layout ([AGENTS.md](../AGENTS.md)): block namespaces, internals not `private`, no nested types, XML docs on internals.
- Coverage: **100% line and 100% branch** fail gate on `CloudNimble.XmlEssentials.dll`. After a hot-path change, run tests **and** `coverage-gate.cs` before declaring a win.
- Do not commit until asked. Do not special-case a benchmark payload (no `if (prefix is "p")`).
- Report reader/writer benches as a markdown table: **Method, Category, Mean, Ratio, Allocated, Alloc ratio**. Integer nanoseconds and integer bytes. `Allocated` `-` from BenchmarkDotNet is **0 B**.

## Process (repeat this)

Do the steps in order. A naive port of one BCL trick without a probe has already **regressed** catalog write time; revert immediately when that happens.

### 1. Measure the real suite

```bash
dotnet run -c Release --project src/CloudNimble.XmlEssentials.Performance -f net11.0 -- --filter *
```

ShortRun + `InProcessEmitToolchain` is what `Program.cs` already uses. Categories today:

| Category | What it is |
| --- | --- |
| `TinyWrite` / `TinyDrain` | `<root id="1"><n>42</n></root>` |
| `CatalogWrite` / `CatalogDrain` | 200 unprefixed items (`item` / `name` / `qty` / `price`) |
| `CatalogGetInt32` | Same catalog; parse integer text (`TryGetInt32` vs BCL `int.TryParse(Value)`) |
| `NamespacedDrain` | 200 items with `xmlns:p="urn:catalog"` and `p:*` names |
| `RealDrain` / `RealDomParse` / `RealDomWrite` | Four fixture documents (RSS, OpenDocument, WordprocessingML, RPM metadata), whitespace and comments kept |
| `DomParseTiny` / `DomParseCatalog` / `DomParseNamespaced` | `XmlDocument.Parse` + `Dispose` on the three payloads above |
| `DomNavigate` | Parsed catalog; each item's `id` attribute and `qty` child as integers |
| `DomWrite` | Parsed catalog; `WriteTo` a reused writer |

XmlEssentials writer benches **reuse** one `Utf8XmlWriter` via `Reset()` (STJ `Utf8JsonWriter.Reset`). BCL `XmlWriter` has no reset; those benches construct per operation. Reader benches construct a `Utf8XmlReader` on a `byte[]` span; BCL reuses a `MemoryStream` with `XmlReaderSettings.CloseInput = false`.

### 2. Split “our object” from “the bench’s strings”

`MemoryDiagnoser` counts everything the iteration allocates, including `i.ToString()` and `"Widget" + i` in the bench body. Before changing the library, run a `GC.GetAllocatedBytesForCurrentThread()` probe (file-based `dotnet run app.cs`, not Python) that measures:

1. `new Utf8XmlWriter` / drain ctor only
2. The catalog loop’s own strings
3. Catalog write/read with numbers formatted into the buffer (`WriteAttributeNumber`, `WriteNumberValue`, `WriteStringValue("Widget")` + number — same XML, no concat)

Catalog write was **872 B writer + 8000 B caller strings**. The 8000 B was not the writer. Changing only the library cannot remove it.

### 3. Read the BCL (and STJ) source that actually runs

Fetch tagged files, for example:

- `https://raw.githubusercontent.com/dotnet/runtime/v11.0.0/src/libraries/System.Private.Xml/src/System/Xml/Core/XmlUtf8RawTextWriter.cs`
- `.../XmlWellFormedWriter.cs`
- `.../XmlWellFormedWriterHelpers.cs` (`ElementScope`, `CheckNCName`, `AttrName`)
- `.../System.Text.Json/src/System/Text/Json/Writer/Utf8JsonWriter.cs` (`GetMemory` / `Advance`, `Reset`)

NameTable / interned prefix compare lives with the reader (`XmlNameTable`, `XmlTextReaderImpl`). That was the namespaced-drain lesson; the reader now gets the same effect from a prefix-to-binding cache ([backlog item 1](#1-prefix-resolution-on-the-reader-done-real-documents-071-to-087)).

### 4. Port the technique, not the extra scans

BCL `RawText` writes UTF-16 ASCII as bytes in **one** loop. A port that does `Ascii.IsValid` + `Latin1.GetBytes` + `AppendNameFromChars` (three scans of `"item"`) **lost** catalog time (1.41× → 1.70×). Revert that class of change.

What **did** work:

| Technique | Where BCL / STJ does it | What we did |
| --- | --- | --- |
| Interned string names on the element stack | `ElementScope` keeps `localName`/`prefix` strings; end tag re-`RawText`s them | `XmlWriterFrame.LocalChars` / `PrefixChars` |
| Complete leaf elements | (we went further than BCL) | `WriteElementString` / `WriteElementNumber` write `<n>v</n>` without a frame |
| ASCII names as bytes | `XmlUtf8RawTextWriter.RawText` | `Ascii.FromUtf16`, `WriteRawByte` for markup |
| Direct `IBufferWriter` | `Utf8JsonWriter` `GetMemory`/`Advance` | No 16 KB staging copy on the `IBufferWriter` ctor |
| Numbers without `ToString` | STJ `Utf8Formatter` | `IUtf8SpanFormattable.TryFormat` into the output span; `WriteAttributeNumber` |
| Static `\n` / `\r\n` | — | Ctor does not `Encoding.UTF8.GetBytes` a newline |
| `Reset` | `Utf8JsonWriter.Reset` | Pool the writer in benches; 0 B after warmup |
| Binding slots, not intern+offset | — | Reader copies `NamespaceBinding` URI start/length onto the element |
| 1-byte prefix | BCL interned `string` pointer | `TryLookupBinding` length filter + 1-byte compare + last-prefix cache |

### 5. Prove it

After **each** behavioral or hot-path change:

```bash
dotnet test src/CloudNimble.XmlEssentials.slnx -c Release --coverage --coverage-output-format cobertura --coverage-output artifacts/coverage/coverage.cobertura.xml
dotnet run src/coverage-gate.cs -- artifacts/coverage/coverage.cobertura.xml
dotnet run -c Release --project src/CloudNimble.XmlEssentials.Performance -f net11.0 -- --filter *Utf8XmlWriterBenchmarks*
# or *Utf8XmlReaderBenchmarks* / --filter *
```

If line coverage drops, add internals tests for the new branch **before** more speed work. If a bench regresses, revert the last trick; do not stack a second trick on a regression.

A `GC.GetAllocatedBytesForCurrentThread` test (see `Utf8XmlWriterAllocationTests.Reset_WriteCatalog_DoesNotAllocate`) is required for any claim of 0 B.

## Baseline (2026-11-22, net11.0 ShortRun, Hyper-V)

Integer ns and bytes. Ratio is XmlEssentials / BCL on that category. These numbers were taken **after** the standards work (strict declaration, reserved namespaces, `]]>` detection, whitespace significance, shared XSD lexical routines, namespace-preserving `WriteTo`) and after backlog item 1. Write benches reset their `ArrayBufferWriter` with `ResetWrittenCount()`, which does not zero the buffer.

### Writer (`Reset` reuse)

| Method | Category | Mean | Ratio | Allocated | Alloc ratio |
| --- | --- | --- | --- | --- | --- |
| BclXmlWriter_WriteCatalog | CatalogWrite | 33321 ns | 1.00 | 13728 B | 1.00 |
| XmlEssentials_WriteCatalog | CatalogWrite | 22871 ns | 0.69 | 0 B | 0.00 |
| BclXmlWriter_WriteTiny | TinyWrite | 343 ns | 1.00 | 7328 B | 1.00 |
| XmlEssentials_WriteTiny | TinyWrite | 57 ns | 0.17 | 0 B | 0.00 |

### Reader, synthetic (new `Utf8XmlReader` per iteration on a `byte[]`)

| Method | Category | Mean | Ratio | Allocated | Alloc ratio |
| --- | --- | --- | --- | --- | --- |
| BclXmlReader_DrainCatalog | CatalogDrain | 26112 ns | 1.00 | 15088 B | 1.00 |
| XmlEssentials_DrainCatalog | CatalogDrain | 18097 ns | 0.69 | 0 B | 0.00 |
| BclXmlReader_DrainCatalogGetInt32 | CatalogGetInt32 | 30281 ns | 1.00 | 35808 B | 1.00 |
| XmlEssentials_DrainCatalogGetInt32 | CatalogGetInt32 | 19074 ns | 0.63 | 0 B | 0.00 |
| BclXmlReader_DrainNamespaced | NamespacedDrain | 21043 ns | 1.00 | 15200 B | 1.00 |
| XmlEssentials_DrainNamespaced | NamespacedDrain | 15250 ns | 0.72 | 0 B | 0.00 |
| BclXmlReader_DrainTiny | TinyDrain | 353 ns | 1.00 | 2472 B | 1.00 |
| XmlEssentials_DrainTiny | TinyDrain | 68 ns | 0.19 | 0 B | 0.00 |

### Reader, DOM, and writer on real documents (`RealDocumentBenchmarks`)

Four fixtures from the quick-xml test suite. Both sides keep whitespace and comments. `RealDomWrite` replays a parsed document into a reused writer. **This is the table that matters.**

| Method | Category | File | Mean | Ratio | Allocated | Alloc ratio |
| --- | --- | --- | --- | --- | --- | --- |
| BclXmlReader_Drain | RealDrain | document.xml | 19870 ns | 1.00 | 25144 B | 1.00 |
| XmlEssentials_Drain | RealDrain | document.xml | 14620 ns | 0.74 | 944 B | 0.04 |
| BclXmlReader_Drain | RealDrain | libreoffice_document.fodt | 80240 ns | 1.00 | 56017 B | 1.00 |
| XmlEssentials_Drain | RealDrain | libreoffice_document.fodt | 51260 ns | 0.64 | 6920 B | 0.12 |
| BclXmlReader_Drain | RealDrain | rpm_primary.xml | 31160 ns | 1.00 | 20064 B | 1.000 |
| XmlEssentials_Drain | RealDrain | rpm_primary.xml | 18850 ns | 0.60 | 80 B | 0.004 |
| BclXmlReader_Drain | RealDrain | sample_rss.xml | 192070 ns | 1.00 | 47137 B | 1.00 |
| XmlEssentials_Drain | RealDrain | sample_rss.xml | 92120 ns | 0.48 | 2073 B | 0.04 |
| BclXmlDocument_Parse | RealDomParse | document.xml | 51680 ns | 1.00 | 127176 B | 1.000 |
| XmlEssentials_Parse | RealDomParse | document.xml | 21160 ns | 0.41 | 1048 B | 0.008 |
| BclXmlDocument_Parse | RealDomParse | libreoffice_document.fodt | 195180 ns | 1.00 | 353889 B | 1.00 |
| XmlEssentials_Parse | RealDomParse | libreoffice_document.fodt | 68990 ns | 0.35 | 7025 B | 0.02 |
| BclXmlDocument_Parse | RealDomParse | rpm_primary.xml | 72480 ns | 1.00 | 170953 B | 1.000 |
| XmlEssentials_Parse | RealDomParse | rpm_primary.xml | 26660 ns | 0.37 | 184 B | 0.001 |
| BclXmlDocument_Parse | RealDomParse | sample_rss.xml | 337530 ns | 1.00 | 709082 B | 1.000 |
| XmlEssentials_Parse | RealDomParse | sample_rss.xml | 124150 ns | 0.37 | 2177 B | 0.003 |
| BclXmlDocument_Write | RealDomWrite | document.xml | 28570 ns | 1.00 | 11032 B | 1.00 |
| XmlEssentials_Write | RealDomWrite | document.xml | 21770 ns | 0.76 | 0 B | 0.00 |
| BclXmlDocument_Write | RealDomWrite | libreoffice_document.fodt | 96930 ns | 1.00 | 23273 B | 1.00 |
| XmlEssentials_Write | RealDomWrite | libreoffice_document.fodt | 71030 ns | 0.73 | 0 B | 0.00 |
| BclXmlDocument_Write | RealDomWrite | rpm_primary.xml | 32480 ns | 1.00 | 8016 B | 1.00 |
| XmlEssentials_Write | RealDomWrite | rpm_primary.xml | 21500 ns | 0.66 | 0 B | 0.00 |
| BclXmlDocument_Write | RealDomWrite | sample_rss.xml | 151090 ns | 1.00 | 8017 B | 1.00 |
| XmlEssentials_Write | RealDomWrite | sample_rss.xml | 78800 ns | 0.52 | 0 B | 0.00 |

What the real documents show:

- Every row is under BCL. Before backlog item 1 the namespace-heavy drains were 1.27× (WordprocessingML) and 1.79× (OpenDocument), and `WriteTo` on the OpenDocument file was 1.22×. The synthetic catalog had hidden all of it.
- **Drain is not 0 B on real documents.** The reader keeps a few arrays per instance (namespace bindings and element stacks once they outgrow their eight inline slots, interned URIs, one buffer for decoded attribute values and one for decoded text). They are allocated per reader, not per token, but a `ref struct` has no `Dispose` and so cannot return a pooled array. The split between them has not been measured. 0 B holds for small, shallow, entity-free documents such as the synthetic ones. This is now the largest gap against the goal (see backlog item 5).
- `RealDomWrite` is 0 B on all four, pinned by `XmlDocumentAllocationTests.WriteTo_RealDocument_DoesNotAllocate`. That test found 200 B per write on the RSS file: sibling elements that declare different prefixes reuse one binding slot in turn, so the slot's remembered bytes never matched. The writer now keeps the first 64 distinct prefixes and URIs it has seen (`InternNamespaceBytes`).

### DOM, synthetic (`XmlDocument` / `XmlElement`, PR 4)

Parse rows build and dispose one document per operation. Navigate and write rows reuse one parsed catalog, so they measure the tree. BCL is `System.Xml.XmlDocument` loaded from a reused `MemoryStream`.

| Method | Category | Mean | Ratio | Allocated | Alloc ratio |
| --- | --- | --- | --- | --- | --- |
| BclXmlDocument_NavigateCatalog | DomNavigate | 3519 ns | 1.00 | 0 B | - |
| XmlEssentials_NavigateCatalog | DomNavigate | 2864 ns | 0.81 | 0 B | - |
| BclXmlDocument_ParseCatalog | DomParseCatalog | 54796 ns | 1.00 | 136128 B | 1.000 |
| XmlEssentials_ParseCatalog | DomParseCatalog | 28707 ns | 0.52 | 104 B | 0.001 |
| BclXmlDocument_ParseNamespaced | DomParseNamespaced | 44048 ns | 1.00 | 110760 B | 1.000 |
| XmlEssentials_ParseNamespaced | DomParseNamespaced | 21604 ns | 0.49 | 104 B | 0.001 |
| BclXmlDocument_ParseTiny | DomParseTiny | 647 ns | 1.00 | 5176 B | 1.00 |
| XmlEssentials_ParseTiny | DomParseTiny | 118 ns | 0.18 | 104 B | 0.02 |
| BclXmlDocument_WriteCatalog | DomWrite | 28341 ns | 1.00 | 7368 B | 1.00 |
| XmlEssentials_WriteCatalog | DomWrite | 15592 ns | 0.55 | 0 B | 0.00 |

The 104 B on parse is the `XmlDocument` object itself. [DOM](06-api-dom.md) rules out a document-instance cache, so parse cannot reach 0 B; everything the document holds is pooled. Before the row-store rewrite the same benches read 0.94 / 1.07 / 0.31 (parse), 3.71 with 59040 B (navigate), and 1.02 (write).

### Serializer, scalar roots (`XmlSerializerBenchmarks`, PR 5)

Each library reads the document it wrote. BCL is a cached `System.Xml.Serialization.XmlSerializer` with reused reader and writer settings; it has no call that returns bytes, so its write rows reuse one `MemoryStream` and copy it out. `System.Text.Json` is a sidecar, not a baseline. XmlEssentials uses `XmlSerializerOptions.Default` (reflection resolver, declaration written).

| Method | Category | Mean | Ratio | Allocated | Alloc ratio |
| --- | --- | --- | --- | --- | --- |
| Bcl_WriteDateTime | WriteDateTime | 340 ns | 1.00 | 7784 B | 1.00 |
| XmlEssentials_WriteDateTime | WriteDateTime | 68 ns | 0.20 | 104 B | 0.01 |
| Bcl_WriteEnum | WriteEnum | 358 ns | 1.00 | 7744 B | 1.00 |
| XmlEssentials_WriteEnum | WriteEnum | 63 ns | 0.18 | 96 B | 0.01 |
| Bcl_WriteInt32 | WriteInt32 | 319 ns | 1.00 | 7728 B | 1.000 |
| Stj_WriteInt32 | WriteInt32 | 31 ns | 0.10 | 32 B | 0.004 |
| XmlEssentials_WriteInt32 | WriteInt32 | 58 ns | 0.18 | 88 B | 0.011 |
| Bcl_WriteInt32Buffer | WriteInt32Buffer | 324 ns | 1.00 | 7648 B | 1.00 |
| XmlEssentials_WriteInt32Buffer | WriteInt32Buffer | 49 ns | 0.15 | 0 B | 0.00 |
| Bcl_WriteInt32String | WriteInt32String | 353 ns | 1.00 | 7784 B | 1.000 |
| Stj_WriteInt32String | WriteInt32String | 34 ns | 0.10 | 32 B | 0.004 |
| XmlEssentials_WriteInt32String | WriteInt32String | 66 ns | 0.19 | 144 B | 0.018 |
| Bcl_WriteNullable | WriteNullable | 595 ns | 1.00 | 8024 B | 1.00 |
| XmlEssentials_WriteNullable | WriteNullable | 86 ns | 0.14 | 144 B | 0.02 |
| Bcl_WriteString | WriteString | 336 ns | 1.00 | 7888 B | 1.00 |
| Stj_WriteString | WriteString | 71 ns | 0.21 | 80 B | 0.01 |
| XmlEssentials_WriteString | WriteString | 88 ns | 0.26 | 128 B | 0.02 |
| Bcl_ReadDateTime | ReadDateTime | 1086 ns | 1.00 | 6040 B | 1.00 |
| XmlEssentials_ReadDateTime | ReadDateTime | 66 ns | 0.06 | 0 B | 0.00 |
| Bcl_ReadEnum | ReadEnum | 819 ns | 1.00 | 4480 B | 1.00 |
| XmlEssentials_ReadEnum | ReadEnum | 60 ns | 0.07 | 0 B | 0.00 |
| Bcl_ReadInt32 | ReadInt32 | 1031 ns | 1.00 | 6048 B | 1.00 |
| Stj_ReadInt32 | ReadInt32 | 25 ns | 0.02 | 0 B | 0.00 |
| XmlEssentials_ReadInt32 | ReadInt32 | 59 ns | 0.06 | 0 B | 0.00 |
| Bcl_ReadInt32Stream | ReadInt32Stream | 1055 ns | 1.00 | 6048 B | 1.00 |
| XmlEssentials_ReadInt32Stream | ReadInt32Stream | 63 ns | 0.06 | 0 B | 0.00 |
| Bcl_ReadString | ReadString | 1193 ns | 1.00 | 6200 B | 1.00 |
| Stj_ReadString | ReadString | 75 ns | 0.06 | 96 B | 0.02 |
| XmlEssentials_ReadString | ReadString | 168 ns | 0.14 | 168 B | 0.03 |

- Every write row allocates only what it returns (the array or the string); `WriteInt32Buffer` writes into the caller's buffer and is 0 B. Pinned by `XmlSerializerAllocationTests` (`Serialize_ToBufferWriter_DoesNotAllocate`, `Serialize_Enum_ToBufferWriter_DoesNotAllocate`, `Serialize_ToUtf8Bytes_AllocatesOnlyTheResult`).
- The 0 B read rows are pinned by `Deserialize_FromUtf8_DoesNotAllocate`, `Deserialize_FromStream_DoesNotAllocate`, `Deserialize_DateTime_FromUtf8_DoesNotAllocate`, and `Deserialize_Enum_FromUtf8_DoesNotAllocate`. `ReadString` allocates the string it returns.
- Before this pass the write rows were **1.40 to 1.65 of BCL and about 17 KB per call**: each call allocated a 16 KB `ArrayBufferWriter`, a `Utf8XmlWriter`, and a copy of the result (twice for a string). Reads were 0.14 to 0.21; `ReadEnum` allocated 24 B and `ReadInt32Stream` 432 B.
- What changed it: one `Utf8XmlWriter` and one pooled buffer per thread (`XmlWriterCache`, used through `XmlWriterLease`, the same idea as STJ's `Utf8JsonWriterCache`); the string and array are produced from the pooled span; streams are read into a pooled array with the size limit checked as bytes arrive; the last type info is checked before the dictionary; enums are read and written from tables of UTF-8 names built once per enum type, with no boxing and no string; dates, times, and GUIDs are formatted on the stack; the root start tag is written from UTF-8 bytes held by the type info (16 ns per element against 22 ns for the `string` overload).

#### Where a single read spends its time (`XmlSerializerSequenceBenchmarks`, `ReadInt32Parts`)

Same value, `<Int32>12345</Int32>`, with and without the 38-byte XML declaration. No baseline: the rows are compared with each other.

| Method | Category | Mean | Allocated |
| --- | --- | --- | --- |
| Reader_Declared | ReadInt32Parts | 44 ns | 0 B |
| Reader_Plain | ReadInt32Parts | 41 ns | 0 B |
| Serializer_Declared | ReadInt32Parts | 58 ns | 0 B |
| Serializer_Plain | ReadInt32Parts | 55 ns | 0 B |
| Serializer_Plain_TypeInfo | ReadInt32Parts | 47 ns | 0 B |

Before the three reader changes below: `Reader_Plain` 57 ns, `Reader_Declared` 121 ns, `Serializer_Plain_TypeInfo` 67 ns, `Serializer_Plain` 78 ns, `Serializer_Declared` 146 ns. A single declared read went from 146 ns to 68 ns.

- **The serializer adds about 6 ns to the tokenizer** (47 against 41) and resolving type info from options adds about 8 ns more.
- **The declaration cost 65 ns and now costs 5 ns.** `ReadPiOrDeclaration` validated the data of a processing instruction one character at a time; a sampling profile had it at 33% of the declared read. Two steps fixed it. It finds `?>` with one search and checks the run against `XmlChars.PlainAsciiChars` with another (65 ns to 22 ns). And the declaration `Utf8XmlWriter` writes, `<?xml version="1.0" encoding="utf-8"?>`, is recognized with one comparison before any parsing (22 ns to 5 ns); every other spelling takes the general path. `System.Xml` does the same with one `StartsWith`.
- **Constructing the reader cost 34 ns and now costs 17 ns.** It was clearing its inline slots. Leaving out the clearing of only the eight small stacks saved 7 ns, which showed that the clearing was the cost and not the size. The slots are guarded by counts and a mask, so they need no clearing, but a slot holding an object reference must be cleared for the collector. So `AttributeRecord` and `NamespaceBinding` no longer hold references: a decoded attribute value is an offset into the one buffer the start tag's attributes share (which now keeps its contents when it grows), and an owned prefix or URI is a negative start that `Utf8XmlReader.Owned` resolves to a well-known namespace or an interned array. The reader went from 1320 to 1064 bytes. Two stacks are still cleared, the element-namespace ones: a slot there is written only once a namespace is in scope and is read as "no namespace" before that. Two earlier attempts were reverted: `this = default` (45 ns, it builds a temporary and copies it) and leaving the fields to the compiler's automatic defaults (no change).
- What `System.Text.Json` does differently, from its source: `Utf8JsonReader` holds a span, a few counters, its options, and a `BitStack`, so there is nothing to clear, and a JSON number is one token where `<Int32>1</Int32>` is three. 25 ns is not reachable for XML; the tokenizer alone is 44 ns here.

#### Sequences (`XmlSerializerSequenceBenchmarks`, 1,000 integers)

BCL has no streaming call, so its baseline is a cached `XmlSerializer` over `int[]`, read and written whole. STJ and XmlEssentials use their asynchronous sequence calls over a rewound `MemoryStream`.

| Method | Category | Mean | Ratio | Allocated | Alloc ratio |
| --- | --- | --- | --- | --- | --- |
| Bcl_ReadArray | ReadSequence | 41141 ns | 1.00 | 61152 B | 1.00 |
| Stj_ReadSequence | ReadSequence | 28082 ns | 0.68 | 8968 B | 0.15 |
| XmlEssentials_ReadSequence | ReadSequence | 32339 ns | 0.79 | 9552 B | 0.16 |
| Bcl_WriteArray | WriteSequence | 32844 ns | 1.00 | 30408 B | 1.00 |
| Stj_WriteSequence | WriteSequence | 13910 ns | 0.42 | 376 B | 0.01 |
| XmlEssentials_WriteSequence | WriteSequence | 21257 ns | 0.65 | 928 B | 0.03 |

- **Both rows are under BCL: 0.79 reading and 0.65 writing.** They were level with it (0.98 to 1.07) until the fixed cost of a small element was cut, see "The fixed cost of a small element" below, and 0.93 to 0.98 and 0.85 until a scalar element was read and written as a leaf, see "Second pass: the element itself".
- `ReadSequence` history: 221981 ns and 545561 B (5.40 of BCL) when streaming first shipped, with a reader state captured and two readers built per value; 65638 ns (1.60) with one state per buffer and every value tokenized twice; 51849 ns (1.26) with one pass and the reader copied after each value; about 43000 ns (1.00 to 1.05) now.
- How it reads in one pass. The built-in value converters have an internal `TryRead` that returns `false` when the reader's block ends before the element does, instead of throwing. A converter that cannot stop part way gets a read-ahead: the element is skipped first, then read from a copy of the reader, which is what `System.Text.Json` does for converters that cannot resume. `Utf8XmlReader.TrySkip` now puts the reader back on the start tag when it returns `false`, as `Utf8JsonReader.TrySkip` does.
- How it resumes without a copy per value. `Utf8JsonReader` is small enough to checkpoint by assignment; ours is 1064 bytes and holds references, so each copy goes through the write-barrier block move. A profile had that copy as the largest single cost. But the same elements are open and the same namespaces are in scope before every child of the document element, so the state is captured once, at the document element's start tag, and moved to where the last complete value ended (`XmlReaderStateSnapshot.At`). That costs three numbers per value.
- Where the time went before a scalar element was read as a leaf (43 µs then, 32 µs now), measured apart: 22 µs in the tokenizer (three tokens per value), 11 µs in the asynchronous iterator itself (a bare iterator of 1,000 integers costs that; STJ pays it too, the BCL array call does not), 1 µs in stream reads, the rest in the loop.
- `WriteSequence` was 33 ns per value before a scalar element was written as a leaf (21 ns now): about 17 ns in the writer per element (20 ns measured alone with a span name, before the next change), 11 ns in the asynchronous enumeration, the rest in per-value checks. One writer change came out of profiling it: `PushFrame` took a whole `XmlWriterFrame` by value and stored it, and because the frame holds two string references that store went through the collector's block move on every start tag. The frame is now built in its slot (`NextFrame`); 3 ns per element, 39.4 to 36.3 µs per 1,000 in a direct timing. Indexing the inline frames through `Unsafe.Add` instead of the indexer was tried and made no difference, so it was not kept. STJ's converter does not suspend per element either (it loops "while elements are being returned synchronously"); its advantage is that a JSON number and a comma cost less than an XML element.

Summary: every category, synthetic and real, is under BCL. Synthetic categories are 0 B (apart from the one object per parsed document, and the result a serializer call returns). Real-document drains allocate 80 B to 6920 B per reader.

#### The fixed cost of a small element

`<Int32>1</Int32>` is three tokens to read and three calls to write, and none of them has enough bytes for a vectorized search to pay for itself. Timed directly on 1,000 such elements (no serializer, no stream): reading went from 21.9 to 19.7 µs and writing from 20.0 to 12.3 µs.

- **Reader: short text is read one byte at a time.** `ReadText` ran three vectorized searches over a text node of one to three bytes. It now reads up to `ShortTextBytes` (8) bytes itself, taking only bytes that mean the same in both XML versions (printable ASCII other than `<`, `&` and `>`, the blank, the tab, the line feed), and hands anything else or anything longer to the existing loop. This also covers the indentation between two tags. The limit was 16 at first; that cost a 20-byte value 4 ns (1,000 date-times: 22.8 µs with no head, 27.0 µs at 16, 24.0 µs at 8) and showed up as `ReadDateTime` going from 77 to 80 ns. At 8 the integers, `rpm_primary.xml`, and the OpenDocument file keep all of the gain.
- **Checking a suspect row.** Five rows looked a few percent slower after this work. Three reruns settled them: `WriteInt32Buffer` and `ReadString` were noise; `ReadDateTime` was the head limit above; the two `sample_rss.xml` rows were the machine, because BCL moved with them and the ratios stayed at 0.48 and 0.37. Compare ratios, not times, across runs made at different moments.
- **Writer: short names are checked one byte at a time.** `ValidateNcName` ran one vectorized search for name characters and a second for a colon. `XmlChars.IsShortAsciiNcName` decides an ASCII name of up to 16 bytes in one loop (20.0 to 17.8 µs).
- **Writer: a tag goes out in one step.** A start tag was `<` and the name in two writes; an end tag was `</`, the name, and `>` in three. `WriteTagOpen` and `WriteTagClose` reserve once and write once (17.8 to 12.1 µs). This was the largest single step.
- **Tried and not kept**, each measured on the same loop: caching the array behind the output `Memory<byte>` so `Memory<T>.Span` is not asked again (12.1 to 15.6 µs, slower); writing the `>` that closes a start tag together with a number (12.1 to 13.1 µs, slower); indexing the inline frames through `Unsafe.Add` (no change).
- The CPU sampling profile was no help here: it charged most samples to whichever native frame was on the stack. What worked was the direct loop and one change at a time.

#### A document that was only fast when the JIT was lucky

The first full run after the reader changes had `sample_rss.xml` at 256 µs to drain (1.36 of BCL) where it had been 161 µs. The committed code still gave 161 µs on the same machine, so the change caused it, yet none of the changed code runs more than three times on that file. Outside BenchmarkDotNet the same build gave 155 µs with tiered compilation and 296 µs with it off. That was the finding: the file is mostly CDATA, `ReadCData` validated one character at a time through `AdvanceChar`, and it was fast only when profile-guided optimization inlined that call. The unrelated change had tipped the decision inside the benchmark process.

`ReadCData` and `ReadComment` now skip a run of plain ASCII with one search (`XmlChars.CDataPlainChars`, `XmlChars.CommentPlainChars`: tab and printable ASCII without the character that can start the terminator) and leave line ends and everything else to `AdvanceChar`. The drain is 89 µs in the benchmark (0.48), 83 µs outside it, and 109 µs with tiering off.

The tests written for that skip found a conformance gap that had been there since the reader shipped: line ends inside CDATA sections, comments, and processing instructions were returned as written, where XML 2.11 (and `spec/02-xml-fidelity.md`) say `#xD#xA` and `#xD` become `#xA` everywhere, and NEL and LS too in XML 1.1. `DecodeValueSpan` now takes `expandReferences` and all four token kinds go through it; a value with no carriage return (or 1.1 line end) still stays in the input, at the cost of one vectorized search per token.

Process rule that follows: **when a row moves by a large amount for no visible reason, time it outside BenchmarkDotNet with `DOTNET_TieredCompilation=0`.** If the untiered number is much worse than the tiered one, the hot path depends on a JIT decision and should be fixed so it does not.

### Serializer, objects (`XmlSerializerObjectBenchmarks`, PR 6)

Each library reads the document it wrote and writes into a sink that is reused between calls. The models are in `CloudNimble.XmlEssentials.Performance/Models` and carry both libraries' attributes, so the documents have the same shape (the BCL ones are about 100 bytes longer because they declare `xsi` and `xsd`). The lists hold 1,000 items; the dictionary holds 100. The BCL serializer cannot map a record created through its constructor or a dictionary, so `Members` and `Dictionary` have no BCL row.

| Method | Category | Mean | Ratio | Allocated | Alloc ratio |
| --- | --- | --- | --- | --- | --- |
| Bcl_ReadCustomer | ReadCustomer | 8793 ns | 1.00 | 16496 B | 1.00 |
| Stj_ReadCustomer | ReadCustomer | 3022 ns | 0.34 | 3576 B | 0.22 |
| XmlEssentials_ReadCustomer | ReadCustomer | 3367 ns | 0.38 | 3224 B | 0.20 |
| Stj_ReadDictionary | ReadDictionary | 3670 ns |  | 13392 B |  |
| XmlEssentials_ReadDictionary | ReadDictionary | 4003 ns |  | 13392 B |  |
| Bcl_ReadDrawing | ReadDrawing | 209954 ns | 1.00 | 130866 B | 1.00 |
| Stj_ReadDrawing | ReadDrawing | 144239 ns | 0.69 | 117914 B | 0.90 |
| XmlEssentials_ReadDrawing | ReadDrawing | 114338 ns | 0.54 | 90793 B | 0.69 |
| Stj_ReadMembers | ReadMembers | 163536 ns |  | 240274 B |  |
| XmlEssentials_ReadMembers | ReadMembers | 165533 ns |  | 247802 B |  |
| Bcl_ReadMoney | ReadMoney | 268502 ns | 1.00 | 146251 B | 1.00 |
| Stj_ReadMoney | ReadMoney | 92919 ns | 0.35 | 89073 B | 0.61 |
| XmlEssentials_ReadMoney | ReadMoney | 89583 ns | 0.33 | 88601 B | 0.61 |
| Bcl_ReadOrder | ReadOrder | 2044 ns | 1.00 | 7448 B | 1.00 |
| Stj_ReadOrder | ReadOrder | 345 ns | 0.17 | 232 B | 0.03 |
| XmlEssentials_ReadOrder | ReadOrder | 469 ns | 0.23 | 296 B | 0.04 |
| Bcl_ReadOrders | ReadOrders | 784576 ns | 1.00 | 567319 B | 1.00 |
| Stj_ReadOrders | ReadOrders | 330984 ns | 0.42 | 249075 B | 0.44 |
| XmlEssentials_ReadOrders | ReadOrders | 444197 ns | 0.57 | 248667 B | 0.44 |
| Bcl_ReadReadings | ReadReadings | 345448 ns | 1.00 | 267715 B | 1.00 |
| Stj_ReadReadings | ReadReadings | 143904 ns | 0.42 | 105074 B | 0.39 |
| XmlEssentials_ReadReadings | ReadReadings | 195325 ns | 0.57 | 104602 B | 0.39 |
| Bcl_WriteCustomer | WriteCustomer | 4129 ns | 1.00 | 8648 B | 1.00 |
| Stj_WriteCustomer | WriteCustomer | 1482 ns | 0.36 | 312 B | 0.04 |
| XmlEssentials_WriteCustomer | WriteCustomer | 1728 ns | 0.42 | 0 B | 0.00 |
| Stj_WriteDictionary | WriteDictionary | 1252 ns |  | 0 B |  |
| XmlEssentials_WriteDictionary | WriteDictionary | 2386 ns |  | 0 B |  |
| Bcl_WriteDrawing | WriteDrawing | 131666 ns | 1.00 | 30098 B | 1.00 |
| Stj_WriteDrawing | WriteDrawing | 80973 ns | 0.61 | 313 B | 0.01 |
| XmlEssentials_WriteDrawing | WriteDrawing | 70420 ns | 0.53 | 0 B | 0.00 |
| Stj_WriteMembers | WriteMembers | 57350 ns |  | 312 B |  |
| XmlEssentials_WriteMembers | WriteMembers | 71134 ns |  | 0 B |  |
| Bcl_WriteMoney | WriteMoney | 82070 ns | 1.00 | 48009 B | 1.000 |
| Stj_WriteMoney | WriteMoney | 55105 ns | 0.67 | 312 B | 0.006 |
| XmlEssentials_WriteMoney | WriteMoney | 56167 ns | 0.68 | 0 B | 0.000 |
| Bcl_WriteOrder | WriteOrder | 1013 ns | 1.00 | 8192 B | 1.00 |
| Stj_WriteOrder | WriteOrder | 190 ns | 0.19 | 0 B | 0.00 |
| XmlEssentials_WriteOrder | WriteOrder | 273 ns | 0.27 | 0 B | 0.00 |
| Bcl_WriteOrders | WriteOrders | 418925 ns | 1.00 | 214507 B | 1.000 |
| Stj_WriteOrders | WriteOrders | 202642 ns | 0.48 | 314 B | 0.001 |
| XmlEssentials_WriteOrders | WriteOrders | 234070 ns | 0.56 | 0 B | 0.000 |
| Bcl_WriteReadings | WriteReadings | 209447 ns | 1.00 | 131138 B | 1.000 |
| Stj_WriteReadings | WriteReadings | 106860 ns | 0.51 | 313 B | 0.002 |
| XmlEssentials_WriteReadings | WriteReadings | 136989 ns | 0.65 | 0 B | 0.000 |

| Category | What it holds |
| --- | --- |
| `Order` / `Orders` | One flat object of nine scalars (two strings, one with `&`; `int`, `long`, `bool`, enum, `decimal`, `double`, `DateTime`); a list of 1,000 |
| `Customer` | Two child objects, an unwrapped list of 20 objects, a wrapped list of 4 strings |
| `Readings` | 1,000 objects mapped as four attributes |
| `Money` | 1,000 elements with simple content and one attribute |
| `Members` | 1,000 records created through a four-parameter constructor |
| `Drawing` | 1,000 objects of three runtime types, told apart by element name |
| `Dictionary` | 100 integers keyed by name |

- **Every row with a BCL baseline is under it: 0.23 to 0.57 reading and 0.27 to 0.68 writing.** As PR 6 first worked, three rows were over (`WriteOrders` 1.21, `WriteReadings` 1.27, `ReadReadings` 1.51) and `ReadOrders` was 0.92.
- **Writing an object allocates nothing**, pinned by `Serialize_Objects_ToBufferWriter_DoNotAllocate` (elements, attributes, simple content, wrapped and unwrapped lists, arrays, derived types, dictionaries, a root list, a record). Reading allocates the objects and strings it returns: `Deserialize_Person_AllocatesOnlyTheObjectAndItsString`, `Deserialize_AttributeMapped_AllocatesOnlyTheObject`, `Deserialize_Price_AllocatesOnlyTheObjectAndItsString`. Two exceptions: a type created through a constructor with parameters holds its values in an array and boxes its value types until the constructor runs (`ReadMembers`, 8 B per record more than STJ), and a value that needs decoding makes the reader allocate its decode buffer (see the backlog).
- **The scalar rows had slowed by about 15 ns when PR 6 first worked, and the object benchmarks are what showed it.** `XmlSerializerOptions` keeps the type info it returned last and checks it before the dictionary. PR 6 put the dictionary behind a lock, so that a type still being bound is not handed out, and stopped updating that slot on a dictionary hit. After a second type had been used, every root call took the lock: `ReadInt32` was 81 ns and `WriteInt32Buffer` 67 ns in the same process that had run other types, and 63 and 48 ns in a loop that used only `int`. A hit now becomes the type info returned last, unless a type is being bound at that moment. The scalar and sequence tables above are from the run after that fix.

#### What made the difference (first pass)

Times are 1,000 `Order` objects in a direct loop unless a row says otherwise.

| Step | Before | After |
| --- | --- | --- |
| Members read and written as their own type; object created first and set as it is read; members sorted into attributes, content, and elements once; the next expected member tried first; typed lists | read 710 µs, write 494 µs | read 538 µs, write 372 µs |
| Dates and times formatted by hand into the output (`XmlLexical.FormatDateTime` and friends) | `DateTime` 57 ns | 26 ns; write 322 µs |
| An attribute compared by its whole name when it has no prefix; mapped members looked for before the names the serializer reserves | `ReadReadings` 283 µs | 226 µs |
| `GetNil` scans the attribute records directly and skips unprefixed ones; `HandleNull` read once per type | `ReadReadings` 226 µs | 187 µs |
| Decoding a value copies whole runs between references; a decoded run followed by the end tag is parsed where it lies | string with `&` 102 ns | 73 ns |
| Dictionary converter typed by key and value | write 3.7 µs and 8856 B per 100 entries | 2.4 µs and 0 B |

- **Before.** The first version held every member's value in an `object?[]` with a `bool[]` beside it, boxed each value type, searched all members for each element, called the getter twice per member on write, created collections through `Activator` and `IList`, and read an `AsyncLocal` per member for the reference resolver.
- **Now.** `XmlBoundProperty<TDeclaring, TValue>` reads with `XmlTypeInfo<TValue>` and sets through a typed delegate. Whether each member has appeared is a stack-allocated span. `XmlCollectionBinding<TElement>` builds a `List<TElement>` and writes lists and arrays without an enumerator object. The reference resolver is looked at only when `ReferenceHandler` is set. A type created through a constructor with parameters still holds its values until the element ends, as STJ does.
- **Where the rest of the time was, before the second pass.** A hand-written loop over the reader that parses and sets the same members (what a source generator would emit) takes 434 µs for the orders against 517 µs, and 189 µs for the readings against 187 µs. The reader alone takes 267 µs of the orders. So the serializer's own share is small now, and the next gains are in the reader and in parsing: `decimal` costs 24 ns more than `int`, `Guid` 29 ns, `DateTime` 17 ns.
- **Per type before the second pass, 1,000 root items, ns per item (XmlEssentials / BCL / STJ).** Write: `int` 15 / 31 / 3, `bool` 17 / 30 / 2, `double` 51 / 68 / 36, `decimal` 26 / 47 / 18, `DateTime` 26 / 60 / 13, `string` 20 / 30 / 5, string with `&` 36 / 37 / 23, enum 20 / 31 / 3, `Guid` 14 / 44 / 3. Read: `int` 27 / 41 / 9, `double` 48 / 61 / 27, `decimal` 52 / 68 / 23, `DateTime` 45 / 71 / 15, `string` 39 / 144 / 16, string with `&` 73 / 170 / 51, enum 37 / 46 / 8, `Guid` 57 / 78 / 24.

#### Second pass: the element itself

After the first pass the serializer's own share was small, so the second pass went after what each member costs in the writer and the reader. Times are direct loops; rows not named are 1,000 `Order` objects.

| Step | Before | After |
| --- | --- | --- |
| A scalar member is written as a start tag built when the type is bound, the value, and an end tag built the same way, with no element frame (`Utf8XmlWriter.WriteLeafStart` / `WriteLeafEnd`) | write 316 µs; one `int` element 14.6 ns | 213 µs; 7.8 ns |
| An attribute member is written from ` name="` built when the type is bound (`WriteStartAttributeTrusted`); it is still recorded and checked for duplicates | `WriteReadings` 151 µs | 120 µs |
| A scalar element's text and end tag are read in one step (`Utf8XmlReader.TryReadLeafContent`) | one `int` element 27.9 ns; `ReadMembers` 202 µs; `ReadDrawing` 125 µs | 21.5 ns; 167 µs; 108 µs |
| `decimal` written as sign, digits, point, digits with at most 18 digits is parsed into 64 bits directly (`XmlLexical.TryParsePlainDecimal`) | 43 ns | 30 ns |
| A type created through a constructor holds its values in one array laid out in parameter order, and the constructor is called on that array | `ReadMembers` 303802 B, 167 µs | 247802 B, 159 µs |
| A member that is a built-in scalar and has no attributes is read without the nil check and one virtual call (interleaved runs: 430, 419, 421 without; 394, 394, 408, 388, 378 with) | read about 423 µs | about 392 µs |
| A struct's properties are read and set through delegates that take the struct by reference, instead of `PropertyInfo.GetValue` / `SetValue` (1,000 four-member structs) | read 145 µs, write 89 µs | read 118 µs, write 66 µs; the same shape as a class is 120 and 57 µs |

- **Why the leaf paths are safe.** A frame exists to remember an element's name, its namespaces, and whether it has content. A scalar member has a fixed name, no namespaces of its own, and exactly one value, so the writer has nothing to remember; the value is written as text of the parent and `WriteLeafEnd` puts back the one flag that changes (whether the parent holds text, which decides indentation). The name is an ASCII NCName checked when the tag is built; `MaxNameByteCount` and `MaxDepth` are still checked on every write. A member in a namespace, with a non-ASCII name, or whose converter is not built in takes the ordinary path. On the reader, `TryReadLeafContent` only accepts text that is plain in both XML versions (tab, blank, printable ASCII without `<`, `&`, `>`), with a byte that is not white space among the first eight, followed at once by an end tag that repeats the start tag's bytes and `>`. Anything else returns `false` before anything has been read, so every other shape still goes through `Read`.
- **`BCL` generates straight-line code per type and is not faster for it.** `XmlSerializationReaderILGen` emits a stack-allocated `Span<bool>` of members seen and a chain of comparisons of atomized names in declared order, which is the shape this converter already has without generated code.
- **Tried and not kept**, each measured on the same loop: calling the property's own getter and setter delegates through `Unsafe.As` instead of through a closure that casts (no change); a hand-written parse of the hyphenated GUID form (43 to 48 ns per element, slower than `Utf8Parser`).
- **Noise.** This machine moves a 400 µs loop by 5 to 8% between runs. A step is listed only if it was larger than that, or was confirmed by interleaved runs of both builds.
- The reader, writer, DOM, and real-document tables above were not regenerated: the full run after this pass had every one of those rows within noise of the table (the one row that looked different, DOM parse of `document.xml` at 26.5 µs, was 21.1 µs when rerun).

#### Third pass: the backlog

Each backlog item was either done, or measured and left with the reason.

| Item | Outcome | Before | After |
| --- | --- | --- | --- |
| `Nullable<T>` members skipped the leaf paths | **Done.** A nullable whose value has a built-in converter gets `XmlNullableValueConverter<T>`, itself a value converter, so `int?` reads and writes like `int`. A custom converter for `T` is still wrapped by `XmlNullableConverter<T>`. | 1,000 `int?`: write 17.8 ns each | 9.6 ns |
| Writing a nullable from a list boxed it | **Found here and fixed.** `value is null` on a type parameter is a box and a null test. The JIT normally removes the box for a `Nullable<T>`; with profile-guided inlining it did not, in one loop. `XmlConverterHelpers.IsNull` uses the comparer and cannot box. Pinned by `Serialize_NullableValues_ToBufferWriter_DoNotAllocate`, which warms up long enough for tier 1. | 24,000 B per 1,000 `int?` | 0 B |
| Writing a `TimeSpan` allocated a string | **Done.** `XmlLexical.FormatDuration` writes into the output. It also fixes three defects: a duration under one millisecond was written as `P`, sub-millisecond ticks were dropped, and `TimeSpan.MinValue` threw. A fraction now has no trailing zeros (`PT1.5S`, as `XmlConvert` writes it; it was `PT1.500S`). The parser now reads `TimeSpan.MinValue` back. | 42.3 ns, 152 B | 15.9 ns, 0 B |
| The reader's decode buffer was sized exactly | **Done.** It at least doubles. Pinned by `Drain_DecodedValuesThatKeepGrowing_AllocateBuffersThatDouble`. | 64 values growing from 5 to 68 bytes: 64 buffers | 5 buffers |
| A member named like another member's derived type | **Done.** `XmlObjectBinder.ValidateElementNames` throws `InvalidOperationException` when the type is bound, using the case rule the options ask for. | first or expected-next member won | rejected |
| `XmlReferenceScope` is an `AsyncLocal` | **Measured, not changed.** A thread-static field moved a Preserve write of 200 customers from 134 to 127 µs, inside the noise, against 50 µs with no handler. The cost is the resolver: a dictionary lookup, an id string, and a namespaced attribute per object. | | |
| Attribute reads (1.36 of STJ) | **Measured, not changed.** Reading the first eight bytes of an attribute value one at a time, as `ReadText` does for text, was slower (155 to 164 µs for `ReadReadings`): the plain-value search already stops at the closing quote. A hand-written loop over the reader is level with the serializer here, so what is left is the start-tag parser. | | |
| `Guid` and `double` parsing | **Measured, not changed.** Parsing alone is 11 ns for a GUID and 17 ns for a double with `Utf8Parser`, and 9 and 16 ns with the type's own UTF-8 `TryParse`. There are 2 ns to gain. | | |
| Text with a reference | **Measured, not changed.** One `&amp;` in a 35-byte string costs the reader 31 ns (a drain goes from 25 to 56 ns per element) and the serializer 16 ns more for the two-token path. The reader validates the reference in `ReadText` and decodes it again in `DecodeValueSpan`, with about eleven vectorized searches between them. Decoding in one pass is the fix, and it is a rewrite of the reader's most sensitive loop, so it is its own piece of work. | | |
| Boxing in constructor-created types | **Not done.** Removing it needs a typed constructor delegate, which means generated code (PR 7) or `System.Reflection.Emit`, and the second is ruled out for AOT. | | |

#### What the benchmarks and tests found

The object benchmarks were the first code to use several shapes, and the coverage gate then showed that many branches had only ever been reached by accident. Both turned up defects in PR 6 as first written; each now has a test in `XmlSerializerObjectMappingTests`.

- A list of a polymorphic type could not hold an item of the declared type itself: writing it threw `NotSupportedException`. It is written under the member name.
- A member declared as `IDictionary<TKey, TValue>` threw at bind time.
- A dictionary entry whose key is `Key` was written as `<Key>` and read back as the start of a pair. It is written in the pair form.
- A child of a mixed-content element named like the mixed member was read as an object instead of kept as an element.
- A type created through a constructor never registered its `id`, so a later `ref` to it failed.
- An exception thrown by a constructor arrived wrapped in `TargetInvocationException`. It is thrown as it is.
- Not changed, for a decision: an array root is named `Int32Array` while `SerializeAsyncEnumerable<int>` writes `ArrayOfInt32`, so one cannot read the other's document.

## Shipped after PR 4 (reader drain)

| Technique | Where BCL does it | What we did |
| --- | --- | --- |
| End tag equals start tag | `XmlTextReaderImpl` compares interned name references | `TryMatchStartTagName` compares the end tag to the start tag's qualified-name bytes in one `SequenceEqual`. A hit settles local name and prefix together: no name scan, no second local compare, no prefix compare, no namespace lookup. `TryMatchEndName` / `ReadName` remain for a different prefix on the same URI and for resumed elements. `EndPrefixMatchesStart` is gone. |
| Scope pop without copying | — | `PopNamespaceScope` reads the top binding's depth in place (`BindingDepthAt`) and returns on the first mismatch. Was two 40-byte `NamespaceBinding` copies per end tag. |
| Decode only when something needs decoding | `XmlTextReaderImpl.ParseText` tracks entity / EOL state while scanning | `ReadText` already stops at `&`, CR, and XML 1.1 line ends. It now remembers whether it hit one, and skips `DecodeValueSpan` (two more scans) when it did not. |
| One scan for attribute normalization | — | `DecodeAttributeValue` tests `&`, TAB, LF, CR with one `SearchValues` scan. Was four `IndexOf` calls per attribute. |

None of these touch the namespace-binding representation, so the interned-prefix idea below was not needed to get under 1.00.

## Shipped with PRs 2–3

- Writer: interned string names on frames, complete leaf elements, `Ascii.FromUtf16` / `WriteRawByte`, `IBufferWriter.GetMemory`/`Advance`, `TryFormat` numbers, static newlines, `Reset` → 0 B, catalog write 0.76× BCL.
- Reader: binding-slot URI copy, 1-byte prefix lookup + last-prefix cache, matching end tags skip URI re-resolve. Namespaced drain 1.57× → 1.15× BCL, still 0 B.
- Coverage 100% line. Writer/reader conceptual docs and tokenizer benches.

## Shipped with PR 4 (DOM)

| Technique | Where STJ / BCL does it | What we did |
| --- | --- | --- |
| Rows in document order, no links | `JsonDocument.MetadataDb` / `DbRow` (12 bytes, `NumberOfRows`) | `XmlDbRow` is 32 bytes (was 68). Attributes follow their element, children follow the attributes, `EndIndex` skips a subtree. No first/last/next/parent indexes to maintain. |
| Offsets into an owned payload | `JsonDocument` keeps the UTF-8 and stores `Location` | `XmlDocument` owns one pooled copy of the payload. `Locate` records a tokenizer span by address; only entity-decoded values and interned URIs are appended to the extra buffer. Was 3–4 `AppendExtra` copies per node. |
| Pre-sized rows | `MetadataDb.CreateRented` (one token per 12 bytes) | Same estimate. Rows hold no content, so they are returned uncleared; content buffers are cleared on `Dispose`. |
| One pass per start tag | — | `xsi:nil` is detected while the attributes are captured (was a second walk through `GetNil`). Repeated namespace URIs reuse the previous copy. |
| UTF-8 vs UTF-16 name compare | `JsonElement.TextEquals` | `Utf8Equals`: equal lengths can only match as ASCII (`Ascii.Equals`), a shorter UTF-8 form never matches, the rest transcodes on the stack. Was a `string` per sibling scanned. |
| Typed getters on the bytes | `JsonElement.GetInt32` | A single text child is read in place. Mixed content is every text row in the element's row range, concatenated into a pooled scratch. Was `StringBuilder` → `string` → `byte[]`. |
| Leaf elements in one reservation | (the writer already did this for string names) | `WriteCompleteElementUtf8` writes `<n>v</n>` with one `Ensure` and no frame when the text needs no escaping and the name needs no new declaration. `WriteElementString(ReadOnlySpan<byte>, ReadOnlySpan<byte>)` uses it too. |
| One scan to decide "copy verbatim" | `XmlUtf8RawTextWriter` | `AsciiTextWriteChars` / `AsciiAttributeWriteChars`. Element text was two scans; attribute text had no fast path. |
| Namespace bindings without new arrays | — | `PushNs` reuses the popped slot's arrays when the bytes match, so a namespaced `WriteTo` after `Reset` is 0 B (was 72 B per declaration). |

0 B is asserted by `XmlDocumentAllocationTests` for navigation, enumeration, name lookup (ASCII and non-ASCII), the numeric / Boolean / date / GUID getters, and `WriteTo` (plain and namespaced). Members that return `string`, `byte[]`, or an `XmlNode` allocate that result.

### Standards work after the optimization (and what it cost)

Making the reader, writer, and DOM follow the specifications ([XML fidelity](02-xml-fidelity.md), [testing](14-testing.md)) added work to hot paths. Each addition was kept to a store or a branch, and the tables above are the result after all of it:

| Change | Hot-path cost | Kept cheap by |
| --- | --- | --- |
| `]]>` in text is an error | `>` is a text stop | No extra scan: it joins the existing `SearchValues` stop set, and only a `>` checks the two bytes before it. |
| Whitespace significance (mixed content, whitespace-only leaf, `xml:space`) | One flag write per start tag and per text token | Flags share the element-stack slot that already holds the prefix length. |
| Writer escapes TAB / LF / CR / NEL / LS so values round-trip | None on clean values | The verbatim-copy sets (`AsciiTextWriteChars`, `AsciiAttributeWriteChars`, `PlainTextChars`, `PlainAttributeChars`) are allow-lists; one `IndexOfAnyExcept` decides. The escaper now copies clean runs instead of walking rune by rune. |
| DOM writes namespace declarations as parsed | Two passes over an element's attribute rows | Leaf elements still take the one-reservation path; `RegisterNamespace` / `WriteRegisteredNamespace` split validation from output. |
| Typed getters share `XmlLexical` (XSD rules) | Was 1.11× on `DomNavigate` when first routed through it | A digits-only fast path in `TryParseInteger`; the nil check folded into the value lookup. Back to 0.83. |
| Escaped attribute values | Was one array per escaped attribute | One buffer per reader, reused from tag to tag. |

Bugs that surfaced while proving the above, all fixed with tests. The first two came from the optimization pass; the rest from the W3C suite, real documents, split-at-every-byte reads, exact-match assertions, and round-trip properties:

- `Utf8XmlReader`: an element in no namespace reported the namespace of the previous element at the same depth once that element's bindings were popped (the per-element slot was only written while a namespace was in scope). `_elementNsStored` keeps the unprefixed fast path for documents that never use a namespace.
- `XmlDocument`: a non-empty `xsi:nil` element was not pushed as a parent, so its end tag closed its parent instead.
- Reader, resume: after a resumed element closed, the next element at that depth read the closed element's saved name and namespace ("End tag does not match start tag").
- Reader, resume: a multi-byte character split across buffers inside a name, comment, PI, or CDATA was reported as invalid UTF-8.
- Reader: accepted names starting with a colon, a colon in a PI target, a PI target glued to its data, `XML` / `Xml` as a PI target, the `xmlns` prefix on an element, prefixes bound to the reserved `xml` / `xmlns` namespaces, a literal `]]>` in text, CDATA outside the root element, and `&#X…;`.
- Reader: the XML declaration was matched by substring search, so `<?xml garbage?>` passed and `version` was optional.
- Reader, default options: dropped whitespace between inline elements in mixed content (`a <b>b</b> <i>c</i>`) and the content of a whitespace-only leaf (`<c> </c>`); ignored `xml:space`.
- Reader: `LocalNameSpan` on a text, comment, or CDATA token returned the previous token's name.
- Writer: TAB / LF / CR in attribute values and CR in text were written raw and came back changed; control characters in a `string` value were written raw; `Indented` put whitespace inside text-only elements; comments, CDATA, and PIs were not checked for illegal characters; the `xmlns` element prefix and reserved-namespace declarations were accepted.
- DOM `WriteTo`: dropped namespace declarations that were only referenced from content (`xsi:type="p:T"`), dropped `xmlns=""`, reordered attributes, wrote `<e></e>` as `<e/>`, and dropped an attribute named `p:xmlns`.
- DOM getters: parsed values on their own and disagreed with the reader (whitespace, `+`, dates).
- Typed values: `DateTime` accepted any culture-invariant date (`March 5, 2020`); durations accepted `PT1D`.

Coverage is 100% line and 100% branch (7288 lines, 4590 branches after PR 6; 5786 and 3376 after PR 5; 4427 and 2890 after PR 4), with no test that exists only to reach a branch. `coverage-gate.cs` now fails below 100% branch coverage (it used to allow 80%).

## Backlog (do these next)

Work the list in order. Keep 0 B on drain/write. Do not grow the public API unless the bench cannot express the fast path (writer already has `WriteAttributeNumber` / complete elements).

### 1. Prefix resolution on the reader (done: real documents 0.71 to 0.87)

Real namespace-heavy documents were 1.27× and 1.79× BCL on drain. A sampling profile (`dotnet-trace`, the drain loop on each fixture) showed four separate costs, and only the first was the one this item predicted:

| Cost | What it was | What replaced it |
| --- | --- | --- |
| Prefix lookup | A 1-byte last-prefix cache that multi-byte prefixes never hit, then a scan of every in-scope binding comparing bytes, for each prefixed element and attribute | `LookupPrefixIndex`: an 8-slot direct-mapped cache from prefix to binding index, emptied when a binding is pushed or popped. The default namespace has its own slot (`_nsDefaultIndex`). No interning and no new arrays. |
| Attributes resolved twice or more | The duplicate check resolved each prefixed attribute, and `NamespaceUriSpan` resolved it again | `ResolveAttributeNamespaces` runs once per start tag and stores the binding index on `AttributeRecord.NsIndex`. The duplicate check compares indexes before it compares URIs. |
| Multi-byte text rescanned per character | After each non-ASCII character `ReadText` searched for the next `<` again, then decoded and checked one rune | `AdvanceAsciiTextRun` checks the whole run at once: allowed bytes (`TextRunChars`), `Utf8.IsValid`, and no U+FFFE / U+FFFF. A run that fails is left to `AdvanceChar`, which reports the exact error. This was the `.fodt` gap (it holds Markus Kuhn's UTF-8 demo text). |
| Start-tag overhead | Whitespace between attributes went through the full character validator; each attribute value was scanned four times (stop search, character check, whitespace check, decode-trigger check); names used a vectorized search for five-byte inputs | `SkipWhitespaceBytes` advances over space and tab directly. One `AttrPlainDouble` / `AttrPlainSingle` scan proves a value needs no check, no reference expansion, and no normalization. `ReadName` uses a byte loop. This was the WordprocessingML gap. |

The same lookup cost was on the writer (`LookupUri` scanned every binding for each prefixed name) and is fixed the same way (`_nsCacheIndexes`): `RealDomWrite` on the `.fodt` went from 1.22 to 0.83. `WriteElementText` / `WriteAttributeText` copy a valid multi-byte run in one piece (`WriteMultiByteRun`).

Not done: interned prefix handles on `NamespaceBinding`. The cache made them unnecessary.

### 2. Unprefixed catalog drain (done, 0.88×)

Closed by the end-tag and text work in "Shipped after PR 4". Remaining candidates if more is wanted: `ReadName` runs two vector scans for a prefixed name, and `AdvanceAsciiTextRun` re-scans the run that `IndexOfAny` just delimited. Do not add xmlns work to the unprefixed path.

### 3. Writer: interned NCNames for UTF-8 / span APIs

String start tags already keep interned `string` on `XmlWriterFrame`. UTF-8 `WriteStartElement(ReadOnlySpan<byte>)` still `AppendName`s into the name store. A small intern table of recently written names would shrink deep documents. Not on the catalog bench (string API + `Reset`).

### 4. Writer: `XmlEncodedText` in a dedicated bench

STJ benches often use `JsonEncodedText` for repeated property names. Add a category that writes the catalog with `XmlEncodedText.Encode("item")` etc., **in addition to** the string catalog. Do not replace the string catalog; that is the BCL-fair column.

### 5. Reader `Reset` / reuse

**Writer `Reset` shipped in PR 3** (benches reuse one writer at 0 B). `Utf8XmlReader` is a `ref struct`; reuse is “construct again on the same span,” which is already cheap. If a future mutable reader or sequence reader appears, give it `Reset` and a bench that reuses it.

**Open: per-reader allocations on real documents.** Drains of the four fixtures allocate 80 B to 10504 B each (see the real-document table). They are growth of the reader's own arrays past their inline slots, plus decode buffers, and a `ref struct` cannot return a pooled array without a `Dispose`. Measure the split first (bindings and element stacks vs. interned URIs vs. decode buffers), then choose between larger inline capacity, a caller-supplied scratch buffer in `XmlReaderOptions`, and an explicit `Dispose`.

### 6. DOM and serializer

**DOM shipped in PR 4** (`XmlDocumentBenchmarks`, table above). DOM parse time is now mostly the tokenizer (about 21 of 31 µs on the catalog), so any further reader work is also the next DOM parse win. The mutable `Nodes` tree has no bench yet; `XmlNode.Parse` goes through `XmlDocument` and then allocates one object per node by design.

**Scalar serializer benches shipped with PR 5** (`XmlSerializerBenchmarks`, table above): 0.06 to 0.26 of BCL. Open there:

- **Small elements, what is left.** The serializer now reads `<Int32>1</Int32>` in about 21 ns and writes it in about 8 ns as a leaf; through `Read` and `WriteStartElement` the reader and writer still spend about 20 and 12 ns on the tokens alone. The asynchronous iterator (about 11 ns per value) is now the largest single part of both sequence rows, and it is the price of `IAsyncEnumerable<T>`; STJ pays it too.
- **The reader is still 1064 bytes and construction is 17 ns.** What remains of construction is the caller clearing the local and the two stacks that must be cleared. Making reader state a value instead of a heap snapshot (as `JsonReaderState` is) would make resuming allocation-free; today `CurrentState` rebuilds every name and namespace as strings and costs about 49 ns.
- **A converter that cannot resume pays two reader copies per value when streamed** (the read-ahead). The built-in value converters do not. For object graphs (PR 6) decide whether converters become resumable, as STJ made them.
- **`ReadText` still validates multi-byte and entity-heavy text in small steps.** `sample_rss.xml` is under BCL now, but only CDATA and comments got the plain-run skip; check an entity-dense document with tiering off.

**Object benches shipped with PR 6** (`XmlSerializerObjectBenchmarks`, table above): 0.23 to 0.68 of BCL. Open there, largest first:

- **Text with a reference is decoded twice by the reader.** One `&amp;` costs 31 ns in the reader and 16 ns more in the serializer. `ReadText` validates the reference and `DecodeValueSpan` decodes it again. Decode in one pass, into the decode buffer, from the first byte that needs it.
- **Constructor-created types box their value types** until the constructor runs (8 B per record over STJ). This goes away with the typed constructor call the source generator will emit (PR 7).
- **Attributes are the widest gap to STJ.** What is left is in the start-tag parser, which records, resolves, and checks each attribute. A short scalar scan of the value was tried and was slower.
- **Parsing is at the framework's floor for `Guid` and `double`** (11 and 17 ns of `Utf8Parser`). `DateTime` and enums are 13 and 10 ns over `int` in our own code and have not been profiled.
- **Preserve is 2.6 times a plain write** (134 against 50 µs for 200 customers). Each object is looked up twice, once by the member that holds it and once by its own converter; one lookup would do.

The remaining object-graph columns (first call, source-generated context) come with PR 7 and PR 9. Follow [15](15-benchmarks.md): cache BCL `XmlSerializer`, same object graph, STJ as sidecar. Apply this same process (probe allocs, official BCL source, coverage, table).

## Files that matter

| Area | Files |
| --- | --- |
| Writer hot path | `Utf8XmlWriter.cs`, `XmlWriterFrame.cs`, `XmlWriterOptions.cs` |
| Reader hot path | `Utf8XmlReader.cs`, `Utf8XmlReader.Parsing.cs`, `Utf8XmlReader.Namespaces.cs`, `NamespaceBinding.cs` |
| DOM hot path | `XmlDocument.cs`, `XmlElement.cs`, `XmlDbRow.cs` |
| Typed values | `XmlLexical.cs`, `XmlDeclarationSyntax.cs` |
| Serializer hot path | `XmlSerializer.cs`, `Serialization/XmlTypeInfoOfT.cs`, `Serialization/XmlValueConverterOfT.cs`, `Serialization/XmlConverterHelpers.cs`, `Serialization/XmlWriterCache.cs`, `Serialization/XmlEnumConverterOfT.cs`, `XmlStreamBuffer.cs`, `Serialization/XmlObjectConverterOfT.cs`, `Serialization/XmlBoundPropertyOfT.cs`, `Serialization/XmlCollectionBindingOfT.cs`, `Serialization/XmlDictionaryConverterOfT.cs`, `Serialization/XmlPolymorphism.cs` |
| Benches | `Utf8XmlWriterBenchmarks.cs`, `Utf8XmlReaderBenchmarks.cs`, `XmlDocumentBenchmarks.cs`, `RealDocumentBenchmarks.cs`, `XmlSerializerBenchmarks.cs`, `XmlSerializerSequenceBenchmarks.cs`, `XmlSerializerObjectBenchmarks.cs`, `Models/`, `Program.cs` |
| Allocations | `Utf8XmlWriterAllocationTests.cs`, `Utf8XmlReaderAllocationTests.cs`, `XmlDocumentAllocationTests.cs`, `XmlSerializerAllocationTests.cs` |
| Coverage | `src/coverage-gate.cs`, `artifacts/coverage/coverage.cobertura.xml` |

## What not to do

- Triple-scan ASCII names (`IsValid` + `GetBytes` + copy). One pass.
- `Encoding.UTF8.GetBytes` on every name or newline in the ctor.
- `ToString("G17")` for doubles on the writer hot path.
- Growing a 16 KB staging buffer when the destination is `IBufferWriter<byte>`.
- Claiming 0 B because ArrayPool is warm without a `GetAllocatedBytesForCurrentThread` test after `Reset`/drain warmup.
- Reporting benches as `~30µs / 0.76×`. Use the table.
- Decompiling `System.Private.Xml`. Use the GitHub tag.

---

[Index](README.md) · [Previous: PR plan](22-pr-plan.md)
