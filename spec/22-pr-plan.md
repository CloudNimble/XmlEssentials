# PR Plan

Part of the [XmlEssentials spec](README.md).

Incremental, mergeable PRs. Each PR must compile, include tests for the new surface, and not depend on later PRs except as listed.

TreatWarningsAsErrors is on from PR 1. **CS1591** is in `NoWarn` until the runtime library's first public type (PR 2), then XML comments are required on every new public member in that same PR ([K40](01-identity-and-decisions.md#key-decisions)).

Coverage **line 100%** fail and **branch 80%** report start in **PR 2**, not PR 11. Breakdance baselines update on every public-surface PR.

CI (GitHub Actions matrix `net10.0` and `net11.0`), **MTP host**:

```bash
dotnet test src/CloudNimble.XmlEssentials.slnx -c Release --coverage --coverage-output-format cobertura --coverage-output artifacts/coverage/coverage.cobertura.xml
dotnet run src/coverage-gate.cs -- artifacts/coverage/coverage.cobertura.xml
```

Do **not** use `dotnet test --settings coverlet.runsettings` (VSTest DataCollector; ignored by MTP). See [packaging](13-repo-and-packaging.md#coverage-fail-gate-mtp--not-coverletrunsettings).

`src/CloudNimble.XmlEssentials.slnx` and `src/CloudNimble.XmlEssentials.Docs/` **already exist**. PR 1 keeps them. Namespace in docs usings stays `CloudNimble.Text.Xml`.

## PR 1: Repo scaffolding

- **Title:** `chore: scaffold XmlEssentials solution, props, tests, benchmarks`
- **Depends on:** none
- **Files:**
  - `src/Directory.Build.props` (PackageId strip `CloudNimble.`; `IsSourceGeneratorProject` / `IsDocumentationProject` excluded from `IsPrimaryProject`; IVT **only** on `CloudNimble.XmlEssentials` → `CloudNimble.XmlEssentials.Tests`; CS1591 NoWarn until PR 2; `TreatWarningsAsErrors`)
  - `src/Directory.Build.targets`
  - `src/global.json` (SDK 11.0.x, **`test.runner`: Microsoft.Testing.Platform**)
  - `src/.editorconfig`
  - `src/coverage-gate.cs` (parse Cobertura; fail line &lt; 100% on `CloudNimble.XmlEssentials`; PR 1: exit 0 if zero instrumented lines but Cobertura file must exist)
  - `.github/workflows/ci.yml` (matrix net10.0/net11.0; exact commands above)
  - empty runtime / generator / tests / performance projects
  - **Keep** existing `CloudNimble.XmlEssentials.slnx` and `CloudNimble.XmlEssentials.Docs/`
  - root `README.md`
- **Description:** ClaudeEssentials-shaped tree. CI invokes MTP `--coverage` and `coverage-gate.cs` on the empty library (collector must run).

## PR 2a: Utf8XmlReader skeleton

- **Title:** `feat: Utf8XmlReader skeleton, empty elements, text, XmlException`
- **Depends on:** PR 1
- **Files:** reader, options/state snapshot contract, `XmlException`, `XmlName`, `XmlTokenType`, DoS 0-semantics, tests, **coverage-gate.cs now fails on line &lt; 100%**, XML comments, Breakdance baseline
- **Description:** UTF-8, BOM, declaration, empty vs start+end, text, five entities, DTD reject. No namespaces yet.

## PR 2b: Namespaces

- **Title:** `feat: Utf8XmlReader namespace stack`
- **Depends on:** PR 2a
- **Description:** Prefix/default xmlns, `xmlns=""`, unprefixed attributes vs default xmlns.

## PR 2c: Resume

- **Title:** `feat: Utf8XmlReader IsFinalBlock resume`
- **Depends on:** PR 2b
- **Description:** `XmlReaderState` snapshot tests: split inside namespaced start tag, attribute, text.

## PR 2d: xmlconf subset

- **Title:** `test: UTF-8 XML 1.0 and 1.1 conformance subset`
- **Depends on:** PR 2c
- **Description:** Curated W3C xmlconf without DTD. 1.0 and 1.1. EOL (NEL/LS), RestrictedChar, `xmlns:p=""`, NameStartChar.

(PRs 3–4 still depend on the completed reader = 2d.)

## PR 3: Utf8XmlWriter

- **Title:** `feat: Utf8XmlWriter and reader/writer round-trip`
- **Depends on:** PR 2d
- **Files:** writer (`EmitByteOrderMark` default false; `MaxAttributeCount` / `MaxNameByteCount` on options), `XmlEncodedText`, round-trip tests, baseline
- **Description:** `IBufferWriter<byte>` + Stream. Escaping, indent, xmlns auto-declare, `WriteNilValue`/`WriteNilElement`.

## PR 4: DOM

- **Title:** `feat: XmlDocument, XmlElement, and mutable XmlNode tree`
- **Depends on:** PR 3
- **Description:** ArrayPool rental + Dispose. Mixed content. Nested `XmlElement.Enumerator`. Immutable `XmlDeclarationInfo` on `XmlDocument`.

## PR 5: Options, converters, primitive serialize

- **Title:** `feat: XmlSerializerOptions, XmlConverter, primitive XmlSerializer`
- **Depends on:** PR 4
- **Description:** Scalars. Root name via policy (`<Int32>42</Int32>` identity). Null root = xsi:nil. Full [07](07-api-serializer.md) overload families including TValue+context deserialize.

## PR 6a: POCO elements

- **Title:** `feat: POCO element mapping`
- **Depends on:** PR 5
- **Description:** Public properties, naming policy, nested objects, records/init/required/constructor.

## PR 6b: Attributes and simple content

- **Title:** `feat: XmlAttribute and XmlText mapping`
- **Depends on:** PR 6a

## PR 6c: Collections

- **Title:** `feat: collection mapping (unwrapped default, XmlArray wrap)`
- **Depends on:** PR 6b
- **Description:** Unwrapped default, `[XmlArray]` wrap, **`Dictionary<,>` maps** ([K16](01-identity-and-decisions.md#key-decisions)), extension-data bags.

## PR 6d: Nil, polymorphism, extension data

- **Title:** `feat: xsi:nil, polymorphism, XmlExtensionData`
- **Depends on:** PR 6c
- **Description:** Interleave `[XmlPropertyOrder]` test (derived between two base members). Multiple `[XmlElement]` per-member choice.

## PR 6e: Mixed content, references, async enumerable

- **Title:** `feat: XmlMixed, ReferenceHandler, SerializeAsyncEnumerable`
- **Depends on:** PR 6d
- **Description:** `[XmlMixed]` lists; `XmlReferenceHandler.Preserve` / `IgnoreCycles`; `DeserializeAsyncEnumerable` / `SerializeAsyncEnumerable` (`topLevelValues`); `PipeReader`/`PipeWriter`.

## PR 7: Source generator + context

- **Title:** `feat: XmlSerializerContext source generator packed in XmlEssentials`
- **Depends on:** PR 6e
- **Files:** generator (`IsSourceGeneratorProject`, `IsRoslynComponent`, pack both analyzer folders), `[XmlSerializable]`, `Default` singleton, XMLXE007 = BCL `XmlIncludeAttribute`
- **Description:** **Metadata and Serialization** modes (`Default` = both). Interceptors for `XmlSerializer` options overloads.

## PR 7b: XSD generator

- **Title:** `feat: XSD AdditionalFiles generator emitting types and AOT serialize`
- **Depends on:** PR 7
- **Files:** XsdGenerator in the same analyzer DLL, AdditionalFiles tests, XMLXE010–013, [11](11-xsd-source-generation.md)
- **Description:** `.xsd` → POCOs + context + ser/deser via shared emit. No runtime schema. Import only AdditionalFiles.

## PR 8: AOT / trimming tests

- **Title:** `test: Native AOT smoke using XmlSerializerContext`
- **Depends on:** PR 7b

## PR 9: Benchmarks vs BCL XmlSerializer vs STJ

- **Title:** `bench: XmlEssentials vs cached XmlSerializer vs System.Text.Json`
- **Depends on:** PR 7
- **Description:** Cache BCL instance. Label “BCL defaults (wrapped collections)” unless attributes pin infoset. Match indent/declaration. STJ is sidecar.

## PR 10: DotNetDocs site

- **Title:** `docs: Mintlify DotNetDocs site and conceptual guides`
- **Depends on:** PR 6e; ideally after PR 7
- **Files:** **existing** `CloudNimble.XmlEssentials.Docs` (keep; expand guides), `why-xmlessentials.mdx`, quickstart
- **Description:** XML comments already required from PR 2.

## PR 11: Breakdance freeze + coverage slogan check

- **Title:** `test: freeze public API baseline; confirm 100% line gate`
- **Depends on:** PRs 2–8
- **Description:** Not the first time coverage is measured. Confirms `coverage-gate.cs` still fails the build below 100% line. Subsequent API changes update baselines in the **same** PR.

Preview packages (`-preview`) **after PR 11**, not after PR 7 ([rollout](19-operability.md)).

---

[Index](README.md) · [Previous: References](21-references.md) · [Next: Optimization](23-optimization.md)
