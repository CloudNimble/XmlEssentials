# Benchmarks

Part of the [XmlEssentials spec](README.md).

Project: `CloudNimble.XmlEssentials.Performance`, BenchmarkDotNet, `net10.0` and `net11.0` jobs, `MemoryDiagnoser`.

**Columns (same object graph):**

| Method | Notes |
| --- | --- |
| XmlEssentials serialize/deserialize, first call (reflection) | |
| XmlEssentials serialize/deserialize, cached options (reflection) | |
| XmlEssentials serialize/deserialize, source-gen context | |
| BCL `System.Xml.Serialization.XmlSerializer` | **Cache the instance.** Label **“BCL defaults (wrapped collections)”** unless BCL attributes are applied to pin the infoset as close as this mapping allows. Match indent / `OmitXmlDeclaration` with the XmlEssentials column. |
| `System.Text.Json.JsonSerializer` | Sidecar: XML tax vs JSON on the same graph. Not a win/loss for serializer quality. |

Payload: nested graph (person, address, list of orders with line items) ~2–20 KB. Do not micro-benchmark a single `<a>1</a>`. That graph is `XmlSerializerGraphBenchmarks` (`Customer` with two addresses, 20 line items, four tags).

### Graph (`XmlSerializerGraphBenchmarks`, net11.0 ShortRun, 2026-11-23)

BCL is a cached `System.Xml.Serialization.XmlSerializer`. Collection wrapping follows the attributes on `Customer`. XmlEssentials declaration matches BCL (declaration on, no indent). First-call creates a new `XmlSerializerOptions` each iteration. Source-gen uses `GraphXmlContext.Default.Customer`. STJ is a sidecar.

| Method | Category | Mean | Ratio | Allocated | Alloc ratio |
| --- | --- | --- | --- | --- | --- |
| Bcl_ReadCustomer | ReadCustomer | 8648 ns | 1.00 | 16496 B | 1.00 |
| Stj_ReadCustomer | ReadCustomer | 2946 ns | 0.34 | 3576 B | 0.22 |
| XmlEssentials_ReadCustomer | ReadCustomer | 3358 ns | 0.39 | 3168 B | 0.19 |
| XmlEssentials_ReadCustomer_SourceGen | ReadCustomer | 3092 ns | 0.36 | 3104 B | 0.19 |
| XmlEssentials_ReadCustomer_FirstCall | ReadCustomer | 594483 ns |  | 78608 B |  |
| Bcl_WriteCustomer | WriteCustomer | 4089 ns | 1.00 | 8648 B | 1.00 |
| Stj_WriteCustomer | WriteCustomer | 1502 ns | 0.37 | 312 B | 0.04 |
| XmlEssentials_WriteCustomer | WriteCustomer | 1710 ns | 0.42 | 0 B | 0.00 |
| XmlEssentials_WriteCustomer_SourceGen | WriteCustomer | 1257 ns | 0.31 | 0 B | 0.00 |
| XmlEssentials_WriteCustomer_FirstCall | WriteCustomer | 612933 ns |  | 75440 B |  |

Cached reflection write is 0 B (writer cache). Source-gen write is 0 B and **faster than cached reflection** after generated converters call `WriteLeafStart`/`WriteLeafEnd` with prebuilt `"<Name>"u8` tags and `WriteNumberValue` / `WriteStringValue` for scalars. Source-gen read is slightly ahead of cached reflection on this graph. First-call is type-info bind (~0.6 ms, ~75 KB).

Reader and writer tokenizer benches (`Utf8XmlReaderBenchmarks`, `Utf8XmlWriterBenchmarks`) are a separate suite: tiny document, 200-item catalog, namespaced catalog. How to run them, how to report numbers, and the remaining namespaced-drain work are in [optimization](23-optimization.md).

Fairness rules:

- No `new XmlSerializer(typeof(T))` inside `[Benchmark]` (ModernXmlSerializer serialize bench did this; deserialize bench hashed SHA256/MD5 and never called the library).
- Warmup counts for "cached"; a dedicated first-call benchmark uses `IterationSetup` that creates a new options/context.
- Do not include DOM parse in the serializer comparison unless a separate document benchmark.
- “Same logical payload” is the **object graph**, not the same XML bytes or the same work (BCL wraps collections and uses a different inheritance order unless attributes pin it).

---

[Index](README.md) · [Previous: Testing](14-testing.md) · [Next: Documentation](16-documentation.md)
