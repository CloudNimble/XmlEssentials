# Optimization

Part of the [XmlEssentials spec](README.md).

This page is the playbook for making `Utf8XmlReader` / `Utf8XmlWriter` as fast as STJ, and for beating BCL `XmlReader` / `XmlWriter` on the same payloads. It records **how** this repo has been optimizing, **what already shipped** in PR 2–3, and **what the next pass should do**.

Read [benchmarks](15-benchmarks.md) for serializer-column rules. Reader/writer benches live in `src/CloudNimble.XmlEssentials.Performance/` and are the source of truth for tokenizer numbers.

## Constraints that survive every pass

- Zero `System.Xml*` in runtime and tests.
- Official [dotnet/runtime](https://github.com/dotnet/runtime) source at a tagged release (`v11.0.0` while TFMs are net10/net11). **Not** reflection, ILSpy, or decompilation.
- STJ toolbox: spans, `SearchValues`, `Utf8Formatter` / `IUtf8SpanFormattable`, `Ascii.FromUtf16`, `ArrayPool`, `IBufferWriter.GetMemory` / `Advance`. No string concat on the hot path.
- CloudNimble C# layout ([AGENTS.md](../AGENTS.md)): block namespaces, internals not `private`, no nested types, XML docs on internals.
- Coverage: **100% line** fail gate on `CloudNimble.XmlEssentials.dll`; branch ratchet 80% (currently held at ~100% line / ~99.8% branch). After a hot-path change, run tests **and** `coverage-gate.cs` before declaring a win.
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

NameTable / interned prefix compare lives with the reader (`XmlNameTable`, `XmlTextReaderImpl`). That is the remaining namespaced-drain lesson (below).

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

Integer ns and bytes. Ratio is XmlEssentials / BCL on that category.

### Writer (`Reset` reuse)

| Method | Category | Mean | Ratio | Allocated | Alloc ratio |
| --- | --- | --- | --- | --- | --- |
| BclXmlWriter_WriteCatalog | CatalogWrite | 31382 ns | 1.00 | 13728 B | 1.00 |
| XmlEssentials_WriteCatalog | CatalogWrite | 23737 ns | 0.76 | 0 B | 0.00 |
| BclXmlWriter_WriteTiny | TinyWrite | 335 ns | 1.00 | 7328 B | 1.00 |
| XmlEssentials_WriteTiny | TinyWrite | 61 ns | 0.18 | 0 B | 0.00 |

### Reader (new `Utf8XmlReader` per iteration on a `byte[]`)

| Method | Category | Mean | Ratio | Allocated | Alloc ratio |
| --- | --- | --- | --- | --- | --- |
| BclXmlReader_DrainCatalog | CatalogDrain | 24589 ns | 1.00 | 15088 B | 1.00 |
| XmlEssentials_DrainCatalog | CatalogDrain | 25840 ns | 1.05 | 0 B | 0.00 |
| BclXmlReader_DrainCatalogGetInt32 | CatalogGetInt32 | 29363 ns | 1.00 | 35808 B | 1.00 |
| XmlEssentials_DrainCatalogGetInt32 | CatalogGetInt32 | 26356 ns | 0.90 | 0 B | 0.00 |
| BclXmlReader_DrainNamespaced | NamespacedDrain | 23559 ns | 1.00 | 15200 B | 1.00 |
| XmlEssentials_DrainNamespaced | NamespacedDrain | 27079 ns | 1.15 | 0 B | 0.00 |
| BclXmlReader_DrainTiny | TinyDrain | 348 ns | 1.00 | 2472 B | 1.00 |
| XmlEssentials_DrainTiny | TinyDrain | 92 ns | 0.26 | 0 B | 0.00 |

Writer is ahead. Reader is zero-alloc everywhere, ahead on tiny and `TryGetInt32`, close on unprefixed catalog, **behind on prefixed/namespaced drain**.

## Backlog (do these next)

Work the list in order. Keep 0 B on drain/write. Do not grow the public API unless the bench cannot express the fast path (writer already has `WriteAttributeNumber` / complete elements).

### 1. Interned prefixes on the reader (closes NamespacedDrain)

**This is the remaining tokenizer gap.** ~15% slower than BCL on 200 `p:item` / `p:name` / `p:qty` with one `xmlns:p="urn:catalog"` at the root.

BCL `XmlTextReaderImpl` puts prefixes and local names in a `XmlNameTable` (interned `string`). Prefix equality is a reference compare. We compare UTF-8 slices (`TryLookupBinding`, 1-byte cache of last prefix `p`).

What to try, in this order:

1. Read `dotnet/runtime` `v11.0.0` `XmlTextReaderImpl` / `XmlNameTable` / `XmlNamespaceManager` (tagged raw files). Note how they intern prefixes once per distinct prefix, not per element.
2. Keep **zero heap on the drain** after warmup. Interning must hit a small table of `byte[]` already owned (`InternPrefix` / `InternUri` exist). A new `string` per prefix **fails** the 0 B gate.
3. Store a **canonical prefix handle** on `NamespaceBinding` (the interned `byte[]` or an int id). Lookup becomes `ReferenceEquals` or integer id, not `SequenceEqual` on the start-tag slice.
4. Optionally intern the 1-byte prefix `p` on first `xmlns:p` so every later `p:item` reuses that handle. The 1-byte cache (`_nsPrefixCache`) is a stopgap; a table of in-scope prefix handles is the BCL shape.
5. End tags already skip URI re-resolve when `EndPrefixMatchesStart` is true. Do not break `<a:e xmlns:a="u" xmlns:b="u"></b:e>` (same URI, different prefix) — that path must still `ResolveElementNamespace`.
6. Resume (`IsFinalBlock`) already copies names into owned arrays. Interned handles must survive snapshot restore.

Success: `NamespacedDrain` ratio **≤ 1.00** and **0 B**. Re-run the whole reader table so CatalogDrain does not regress.

### 2. Unprefixed catalog drain (1.05×)

Same `Read()` loop as namespaced without prefix work. After (1), re-measure. If still above 1.00, profile `ReadStartElement` / `ReadText` / `TryMatchEndName` vs BCL `XmlUtf8RawTextReader` (tagged source). Likely leftovers: `ApplyNamespaces` virtual call shape, `CheckDuplicateNamespacedAttributes` call-site branch, `IndexOfAny` stop sets. Do not add xmlns work to the unprefixed path.

### 3. Writer: interned NCNames for UTF-8 / span APIs

String start tags already keep interned `string` on `XmlWriterFrame`. UTF-8 `WriteStartElement(ReadOnlySpan<byte>)` still `AppendName`s into the name store. A small intern table of recently written names would shrink deep documents. Not on the catalog bench (string API + `Reset`).

### 4. Writer: `XmlEncodedText` in a dedicated bench

STJ benches often use `JsonEncodedText` for repeated property names. Add a category that writes the catalog with `XmlEncodedText.Encode("item")` etc., **in addition to** the string catalog. Do not replace the string catalog; that is the BCL-fair column.

### 5. Reader `Reset` / reuse

Writer 0 B required `Reset`. `Utf8XmlReader` is a `ref struct`; reuse is “construct again on the same span,” which is already cheap. If a future mutable reader or sequence reader appears, give it `Reset` and a bench that reuses it.

### 6. DOM and serializer

No DOM/serializer benches exist yet. When they land, follow [15](15-benchmarks.md): cache BCL `XmlSerializer`, same object graph, STJ as sidecar. Apply this same process (probe allocs, official BCL source, coverage, table).

## Files that matter

| Area | Files |
| --- | --- |
| Writer hot path | `Utf8XmlWriter.cs`, `XmlWriterFrame.cs`, `XmlWriterOptions.cs` |
| Reader hot path | `Utf8XmlReader.cs`, `Utf8XmlReader.Parsing.cs`, `Utf8XmlReader.Namespaces.cs`, `NamespaceBinding.cs` |
| Benches | `Utf8XmlWriterBenchmarks.cs`, `Utf8XmlReaderBenchmarks.cs`, `Program.cs` |
| Allocations | `Utf8XmlWriterAllocationTests.cs`, `Utf8XmlReaderAllocationTests.cs` |
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
