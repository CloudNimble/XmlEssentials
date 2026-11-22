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

Payload: nested graph (person, address, list of orders with line items) ~2–20 KB. Do not micro-benchmark a single `<a>1</a>`.

Reader and writer tokenizer benches (`Utf8XmlReaderBenchmarks`, `Utf8XmlWriterBenchmarks`) are a separate suite: tiny document, 200-item catalog, namespaced catalog. How to run them, how to report numbers, and the remaining namespaced-drain work are in [optimization](23-optimization.md).

Fairness rules:

- No `new XmlSerializer(typeof(T))` inside `[Benchmark]` (ModernXmlSerializer serialize bench did this; deserialize bench hashed SHA256/MD5 and never called the library).
- Warmup counts for "cached"; a dedicated first-call benchmark uses `IterationSetup` that creates a new options/context.
- Do not include DOM parse in the serializer comparison unless a separate document benchmark.
- “Same logical payload” is the **object graph**, not the same XML bytes or the same work (BCL wraps collections and uses a different inheritance order unless attributes pin it).

---

[Index](README.md) · [Previous: Testing](14-testing.md) · [Next: Documentation](16-documentation.md)
