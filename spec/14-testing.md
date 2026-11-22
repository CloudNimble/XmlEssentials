# Testing strategy

Part of the [XmlEssentials spec](README.md).

MSTest 4, FluentAssertions 6, method-level parallel, **MTP** host, `Breakdance.Assemblies` 8.* baselines, `InternalsVisibleTo` the test assembly.

## Rules

1. **Specifications are the oracle.** XML 1.0 Fifth Edition, XML 1.1 Second Edition, Namespaces in XML 1.0 / 1.1, and XML Schema Part 2. [XML fidelity](02-xml-fidelity.md) lists every deliberate departure. A test may not encode behavior those documents contradict; if the parser accepts what they reject, the parser is wrong.
2. **Assertions are exact.** Output is compared whole: `.Should().Be(...)` on the complete string, `SequenceEqual` on the complete bytes, `.Should().Equal(...)` on the complete token list. `Contain`, `StartWith`, `EndWith`, `Match`, `IndexOf`, and length-only checks on XML output are not allowed. They pass when attributes are reordered, namespaces dropped, whitespace injected, or text double-escaped, and each of those was a real defect here that a substring assertion hid. The one exception is locating a specific portion of a large fixture document, and a pinned count or digest is still preferred.
3. **Properties over examples.** A handful of hand-picked inputs is not a test of a tokenizer. Every reader change must hold at every byte split; every writer and DOM change must round-trip.
4. **Independent expectations.** Expected values for real documents come from a different implementation (`System.Xml.XmlReader`, run in a script outside the runtime and the tests), never from this library's own output.
5. **No coverage-only tests.** The 100% gate is a floor, not a goal. A line that no meaningful input reaches is dead code and is deleted.
6. No mocks. No `System.Xml` in the test assembly.

## Suites

| Suite | Tests | Content |
| --- | --- | --- |
| W3C conformance | `XmlConformanceSuiteTests` | The W3C XML Conformance Test Suite manifests for XML 1.1 and Namespaces 1.0 / 1.1 (283 cases run: XML 1.0 from xmltest, Sun, IBM, and the Edinburgh errata; XML 1.1; Namespaces 1.0 and 1.1), manifests read with `Utf8XmlReader`. `not-wf` and `error` must throw; `valid` and `invalid` must read, read identically in 1- and 3-byte chunks, and survive `WriteTo`. 205 are UTF-8 without a DTD and are judged on well-formedness; the 78 that need a DTD or another encoding must be rejected with the documented message. Cases whose `EDITION` excludes 5 must read, because the Fifth Edition relaxed those name rules. |
| Conformance (curated) | `Utf8XmlReaderXmlConfTests`, `...WellFormedTests`, `...MalformedTests` | Inline cases for EOL (NEL/LS in 1.1), attribute-value normalization, NameStartChar, RestrictedChar, prefix undeclaration, and every rejected construct. |
| Real documents | `XmlFixtureDocumentTests` | 14 documents from the quick-xml and pugixml test suites (WordprocessingML, OpenDocument, RSS, RPM metadata, config, a 10,001-element file). Element / attribute / comment / PI counts and SHA-256 digests of names, attributes, and text are pinned from an independent reader. Each is also read in 1-, 7-, 251-, and 4096-byte chunks and written back through the DOM. Five more are rejection cases (KOI8-R, Latin-1, DOCTYPE). |
| Reader resumability | `Utf8XmlReaderSplitTests`, `Utf8XmlReaderResumeTests` | Documents split at **every byte offset**; the resumed token stream must equal the single pass. Covers default-namespace nesting and undeclaration, two prefixes on one URI, character references, 1.1 constructs, and multi-byte characters in names, text, comments, PIs, CDATA, and attribute values. |
| Round trip | `XmlRoundTripTests` | Writer → reader: 3000 seeded values per XML version, built from every character that needs care (markup, quotes, TAB/LF/CR, `]]>`, NEL/LS, non-BMP, 1.1 RestrictedChar), written as text and attribute from `string` and UTF-8, and read back exactly; the DOM then writes the same bytes. DOM → writer: documents that must come back byte for byte, and documents where only non-infoset details may change. |
| Robustness | `XmlRoundTripTests.Reader_MalformedInput_OnlyEverThrowsXmlException` | 20,000 seeded mutations. Only `XmlException` may escape; a split read must agree with the single pass on accept, reject, and tokens; an accepted document must be stable under `WriteTo`. |
| Typed values | `XmlLexicalTests` | Valid and invalid lexical forms for every XSD type, each run through `XmlLexical`, `Utf8XmlReader`, and `XmlElement`. |
| Writer | `Utf8XmlWriter*Tests` | Exact output; default **no** BOM; `EmitByteOrderMark` emits `EF BB BF` once then declaration; `MaxAttributeCount` / `MaxNameByteCount`; indentation never inside character data. |
| DOM | `XmlDocument*Tests`, `XmlNodeTests` | Nested `XmlElement.Enumerator`; `XmlDeclarationInfo`; Dispose throws; document-order row store. |
| Allocation | `*AllocationTests` | `GC.GetAllocatedBytesForCurrentThread` around drain, write, navigate, typed getters, and `WriteTo`. A 0 B claim needs one of these. |
| XSD gen | (PR 7b) | AdditionalFiles fixture `.xsd` → types + AOT serialize via context; import not in AdditionalFiles → XMLXE010. |
| Mapping | (PR 6) | Collections, **dictionary maps**, interleave, `[XmlMixed]`, `ReferenceHandler`. |
| Source-gen | (PR 7) | `Default` singleton; XMLXE002 carve-out; XMLXE007 = BCL `[XmlInclude]`. |
| AOT smoke | (PR 8) | PublishAot via context. |
| Public API | Breakdance | On each public-surface PR. |
| No BCL XML | `ForbiddenReferencesTests` | Assembly-ref + `using System.Xml` ban. |
| Coverage | `coverage-gate.cs` | **MTP:** `dotnet test --coverage --coverage-output-format cobertura --coverage-output artifacts/coverage/coverage.cobertura.xml` then `dotnet run src/coverage-gate.cs -- artifacts/coverage/coverage.cobertura.xml`. Line 100% fail from PR 2; branch reported, currently 100%. **Not** `coverlet.runsettings`. |

Fixtures live in `src/CloudNimble.XmlEssentials.Tests/Fixtures` with their sources and licenses in `Fixtures/README.md`. `.gitattributes` marks the folder `-text`: the files are byte-exact inputs.

Do not parse expected XML with `XDocument` in unit tests. Coverage wiring: [packaging](13-repo-and-packaging.md#coverage-fail-gate-mtp--not-coverletrunsettings).

## Known gaps

- Of the 2,238 cases in the W3C suite, 283 run. Almost all the rest need a DTD, which this library rejects by design; the suite cannot say whether the content after the DTD would have been handled correctly.
- `RealDocumentBenchmarks` covers four real documents and shows the reader and `WriteTo` behind BCL on the namespace-heavy ones ([optimization](23-optimization.md)). The writer is only driven through `WriteTo` there: no public UTF-8 overload writes a prefixed attribute, so a reader-to-writer copy bench cannot be written against the public surface.

---

[Index](README.md) · [Previous: Repo and packaging](13-repo-and-packaging.md) · [Next: Benchmarks](15-benchmarks.md)
