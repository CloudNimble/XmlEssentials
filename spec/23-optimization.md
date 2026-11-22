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
| BclXmlWriter_WriteCatalog | CatalogWrite | 33976 ns | 1.00 | 13728 B | 1.00 |
| XmlEssentials_WriteCatalog | CatalogWrite | 24147 ns | 0.71 | 0 B | 0.00 |
| BclXmlWriter_WriteTiny | TinyWrite | 320 ns | 1.00 | 7328 B | 1.00 |
| XmlEssentials_WriteTiny | TinyWrite | 61 ns | 0.19 | 0 B | 0.00 |

### Reader, synthetic (new `Utf8XmlReader` per iteration on a `byte[]`)

| Method | Category | Mean | Ratio | Allocated | Alloc ratio |
| --- | --- | --- | --- | --- | --- |
| BclXmlReader_DrainCatalog | CatalogDrain | 25425 ns | 1.00 | 15088 B | 1.00 |
| XmlEssentials_DrainCatalog | CatalogDrain | 20091 ns | 0.79 | 0 B | 0.00 |
| BclXmlReader_DrainCatalogGetInt32 | CatalogGetInt32 | 30135 ns | 1.00 | 35808 B | 1.00 |
| XmlEssentials_DrainCatalogGetInt32 | CatalogGetInt32 | 21485 ns | 0.71 | 0 B | 0.00 |
| BclXmlReader_DrainNamespaced | NamespacedDrain | 20740 ns | 1.00 | 15200 B | 1.00 |
| XmlEssentials_DrainNamespaced | NamespacedDrain | 16805 ns | 0.81 | 0 B | 0.00 |
| BclXmlReader_DrainTiny | TinyDrain | 316 ns | 1.00 | 2472 B | 1.00 |
| XmlEssentials_DrainTiny | TinyDrain | 89 ns | 0.28 | 0 B | 0.00 |

### Reader, DOM, and writer on real documents (`RealDocumentBenchmarks`)

Four fixtures from the quick-xml test suite. Both sides keep whitespace and comments. `RealDomWrite` replays a parsed document into a reused writer. **This is the table that matters.**

| Method | Category | File | Mean | Ratio | Allocated | Alloc ratio |
| --- | --- | --- | --- | --- | --- | --- |
| BclXmlReader_Drain | RealDrain | document.xml | 19310 ns | 1.00 | 25144 B | 1.00 |
| XmlEssentials_Drain | RealDrain | document.xml | 15830 ns | 0.82 | 1456 B | 0.06 |
| BclXmlReader_Drain | RealDrain | libreoffice_document.fodt | 80280 ns | 1.00 | 56017 B | 1.00 |
| XmlEssentials_Drain | RealDrain | libreoffice_document.fodt | 57300 ns | 0.71 | 10504 B | 0.19 |
| BclXmlReader_Drain | RealDrain | rpm_primary.xml | 30620 ns | 1.00 | 20064 B | 1.000 |
| XmlEssentials_Drain | RealDrain | rpm_primary.xml | 26500 ns | 0.87 | 80 B | 0.004 |
| BclXmlReader_Drain | RealDrain | sample_rss.xml | 188460 ns | 1.00 | 47137 B | 1.00 |
| XmlEssentials_Drain | RealDrain | sample_rss.xml | 162190 ns | 0.86 | 2073 B | 0.04 |
| BclXmlDocument_Parse | RealDomParse | document.xml | 49330 ns | 1.00 | 127176 B | 1.00 |
| XmlEssentials_Parse | RealDomParse | document.xml | 22150 ns | 0.45 | 1562 B | 0.01 |
| BclXmlDocument_Parse | RealDomParse | libreoffice_document.fodt | 188430 ns | 1.00 | 353889 B | 1.00 |
| XmlEssentials_Parse | RealDomParse | libreoffice_document.fodt | 74660 ns | 0.40 | 10649 B | 0.03 |
| BclXmlDocument_Parse | RealDomParse | rpm_primary.xml | 68790 ns | 1.00 | 170953 B | 1.000 |
| XmlEssentials_Parse | RealDomParse | rpm_primary.xml | 29810 ns | 0.43 | 187 B | 0.001 |
| BclXmlDocument_Parse | RealDomParse | sample_rss.xml | 324770 ns | 1.00 | 709082 B | 1.000 |
| XmlEssentials_Parse | RealDomParse | sample_rss.xml | 193750 ns | 0.60 | 2506 B | 0.004 |
| BclXmlDocument_Write | RealDomWrite | document.xml | 28960 ns | 1.00 | 11032 B | 1.00 |
| XmlEssentials_Write | RealDomWrite | document.xml | 26770 ns | 0.92 | 0 B | 0.00 |
| BclXmlDocument_Write | RealDomWrite | libreoffice_document.fodt | 97490 ns | 1.00 | 23273 B | 1.00 |
| XmlEssentials_Write | RealDomWrite | libreoffice_document.fodt | 81140 ns | 0.83 | 0 B | 0.00 |
| BclXmlDocument_Write | RealDomWrite | rpm_primary.xml | 33100 ns | 1.00 | 8016 B | 1.00 |
| XmlEssentials_Write | RealDomWrite | rpm_primary.xml | 25410 ns | 0.77 | 0 B | 0.00 |
| BclXmlDocument_Write | RealDomWrite | sample_rss.xml | 150680 ns | 1.00 | 8017 B | 1.00 |
| XmlEssentials_Write | RealDomWrite | sample_rss.xml | 89830 ns | 0.60 | 0 B | 0.00 |

What the real documents show:

- Every row is under BCL. Before backlog item 1 the namespace-heavy drains were 1.27× (WordprocessingML) and 1.79× (OpenDocument), and `WriteTo` on the OpenDocument file was 1.22×. The synthetic catalog had hidden all of it.
- **Drain is not 0 B on real documents.** The reader keeps a few arrays per instance (namespace bindings and element stacks once they outgrow their eight inline slots, interned URIs, one buffer for decoded attribute values and one for decoded text). They are allocated per reader, not per token, but a `ref struct` has no `Dispose` and so cannot return a pooled array. The split between them has not been measured. 0 B holds for small, shallow, entity-free documents such as the synthetic ones. This is now the largest gap against the goal (see backlog item 5).
- `RealDomWrite` is 0 B on all four, pinned by `XmlDocumentAllocationTests.WriteTo_RealDocument_DoesNotAllocate`. That test found 200 B per write on the RSS file: sibling elements that declare different prefixes reuse one binding slot in turn, so the slot's remembered bytes never matched. The writer now keeps the first 64 distinct prefixes and URIs it has seen (`InternNamespaceBytes`).

### DOM, synthetic (`XmlDocument` / `XmlElement`, PR 4)

Parse rows build and dispose one document per operation. Navigate and write rows reuse one parsed catalog, so they measure the tree. BCL is `System.Xml.XmlDocument` loaded from a reused `MemoryStream`.

| Method | Category | Mean | Ratio | Allocated | Alloc ratio |
| --- | --- | --- | --- | --- | --- |
| BclXmlDocument_ParseCatalog | DomParseCatalog | 52304 ns | 1.00 | 136128 B | 1.000 |
| XmlEssentials_ParseCatalog | DomParseCatalog | 29507 ns | 0.56 | 104 B | 0.001 |
| BclXmlDocument_ParseNamespaced | DomParseNamespaced | 42354 ns | 1.00 | 110760 B | 1.000 |
| XmlEssentials_ParseNamespaced | DomParseNamespaced | 23330 ns | 0.55 | 104 B | 0.001 |
| BclXmlDocument_ParseTiny | DomParseTiny | 642 ns | 1.00 | 5176 B | 1.00 |
| XmlEssentials_ParseTiny | DomParseTiny | 134 ns | 0.21 | 104 B | 0.02 |
| BclXmlDocument_NavigateCatalog | DomNavigate | 3470 ns | 1.00 | 0 B | - |
| XmlEssentials_NavigateCatalog | DomNavigate | 2781 ns | 0.80 | 0 B | - |
| BclXmlDocument_WriteCatalog | DomWrite | 28066 ns | 1.00 | 7368 B | 1.00 |
| XmlEssentials_WriteCatalog | DomWrite | 19510 ns | 0.70 | 0 B | 0.00 |

The 104 B on parse is the `XmlDocument` object itself. [DOM](06-api-dom.md) rules out a document-instance cache, so parse cannot reach 0 B; everything the document holds is pooled. Before the row-store rewrite the same benches read 0.94 / 1.07 / 0.31 (parse), 3.71 with 59040 B (navigate), and 1.02 (write).

Summary: every category, synthetic and real, is under BCL. Synthetic categories are 0 B (apart from the one object per parsed document). Real-document drains allocate 80 B to 10504 B per reader.

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

Coverage is 100% line and 100% branch (4427 lines, 2890 branches), with no test that exists only to reach a branch. `coverage-gate.cs` now fails below 100% branch coverage (it used to allow 80%).

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

No serializer benches exist yet. When they land, follow [15](15-benchmarks.md): cache BCL `XmlSerializer`, same object graph, STJ as sidecar. Apply this same process (probe allocs, official BCL source, coverage, table).

## Files that matter

| Area | Files |
| --- | --- |
| Writer hot path | `Utf8XmlWriter.cs`, `XmlWriterFrame.cs`, `XmlWriterOptions.cs` |
| Reader hot path | `Utf8XmlReader.cs`, `Utf8XmlReader.Parsing.cs`, `Utf8XmlReader.Namespaces.cs`, `NamespaceBinding.cs` |
| DOM hot path | `XmlDocument.cs`, `XmlElement.cs`, `XmlDbRow.cs` |
| Typed values | `XmlLexical.cs`, `XmlDeclarationSyntax.cs` |
| Benches | `Utf8XmlWriterBenchmarks.cs`, `Utf8XmlReaderBenchmarks.cs`, `XmlDocumentBenchmarks.cs`, `RealDocumentBenchmarks.cs`, `Program.cs` |
| Allocations | `Utf8XmlWriterAllocationTests.cs`, `Utf8XmlReaderAllocationTests.cs`, `XmlDocumentAllocationTests.cs` |
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
