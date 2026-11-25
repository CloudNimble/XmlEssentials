# Alternatives considered

Part of the [XmlEssentials spec](README.md).

## 1. Source-generate onto `System.Xml.XmlReader` / `XmlWriter`

Rejected: BCL XML is out of scope for this product.

## 2. Namespace `CloudNimble.XmlEssentials` / package `CloudNimble.XmlEssentials`

Rejected as **namespace**. Assembly **is** `CloudNimble.XmlEssentials`. Namespace stays `CloudNimble.Text.Xml`. Package stays `XmlEssentials`.

## 3–8

Unchanged: no BCL attributes; no `XmlDataSerializer`; no community libs as deps; not EasyAF; not ModernXmlSerializer.

## 9. Wrapped collections as default (BCL) vs unwrapped (STJ)

**Accepted unwrapped** ([K8](01-identity-and-decisions.md#key-decisions)).

## 10. XSD extension order vs flattened + `[XmlPropertyOrder]`

**Accepted:** default base-then-derived; interleave opt-in ([K9](01-identity-and-decisions.md#key-decisions)).

## 11. `Utf8XmlReader` as `ref struct` vs `class`

**Accepted `ref struct`.** Resume state is a heap snapshot inside `XmlReaderState`.

## 12. Coverage bar and test host

**Accepted MTP** (match ClaudeEssentials `global.json`). Collection: `Microsoft.Testing.Extensions.CodeCoverage` (`dotnet test --coverage`). Fail gate: `coverage-gate.cs` (the coverage extension does **not** fail on line 100%). Rejected: VSTest `coverlet.runsettings` under MTP.

## 13. Separate generator package vs in-nupkg

**Accepted in-nupkg** ([K17](01-identity-and-decisions.md#key-decisions)).

## 14. `netstandard2.0` runtime vs net10/net11

**Accepted net10/net11** runtime; generator `netstandard2.0`.

## 15. Metadata-only source-gen

Both **Serialization** and **Metadata** modes ship. `Default` = `Metadata | Serialization`. See [10](10-source-generation.md#serialization-vs-metadata-stj-terms).

## Ideas taken from #125299

Span reader/writer, ROS + `IsFinalBlock`, context source-gen, `OmitXmlDeclaration`, namespace-aware names, `WriteNil`, converters, `IBufferWriter<byte>`. Dropped: `System.Text.Xml`, `XmlData*`, BCL shipping.

---

[Index](README.md) · [Previous: Documentation](16-documentation.md) · [Next: Security](18-security.md)
