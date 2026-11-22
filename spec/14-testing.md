# Testing strategy

Part of the [XmlEssentials spec](README.md).

MSTest 4, FluentAssertions 6, method-level parallel, **MTP** host, `Breakdance.Assemblies` 8.* baselines, `InternalsVisibleTo` the test assembly.

## Suites

| Suite | Content |
| --- | --- |
| Conformance | XML 1.0 and 1.1 utf-8 without DTD. EOL (NEL/LS in 1.1), AVN, NameStartChar, RestrictedChar, prefix undeclaration. |
| XSD gen | AdditionalFiles fixture `.xsd` → types + AOT serialize via context; import not in AdditionalFiles → XMLXE010. |
| Reader resumability | `IsFinalBlock` splits including namespaced start tags. |
| Writer round-trip | Infoset equality; default **no** BOM; `EmitByteOrderMark` emits `EF BB BF` once then declaration; `MaxAttributeCount`/`MaxNameByteCount` on writer. |
| DOM | Nested `XmlElement.Enumerator`; `XmlDeclarationInfo`; Dispose throws. |
| Mapping | Collections, **dictionary maps**, interleave, `[XmlMixed]`, `ReferenceHandler`. |
| Source-gen | `Default` singleton; XMLXE002 carve-out; XMLXE007 = BCL `[XmlInclude]`. |
| AOT smoke | PublishAot via context. |
| Public API | Breakdance on each public-surface PR. |
| No BCL XML | Assembly-ref + `using System.Xml` ban. |
| Coverage | **MTP:** `dotnet test --coverage --coverage-output-format cobertura --coverage-output artifacts/coverage/coverage.cobertura.xml` then `dotnet run src/coverage-gate.cs -- artifacts/coverage/coverage.cobertura.xml`. Line 100% fail from PR 2; branch 80% report. **Not** `coverlet.runsettings`. |

Do not parse expected XML with `XDocument` in unit tests. Coverage wiring: [packaging](13-repo-and-packaging.md#coverage-fail-gate-mtp--not-coverletrunsettings).

---

[Index](README.md) · [Previous: Repo and packaging](13-repo-and-packaging.md) · [Next: Benchmarks](15-benchmarks.md)
