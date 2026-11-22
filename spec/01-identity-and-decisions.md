# Product identity and key decisions

Part of the [XmlEssentials spec](README.md).

## Product identity

| Item | Value | Notes |
| --- | --- | --- |
| Repo | `D:\GitHub\XmlEssentials` | Own repo, not EasyAF. LICENSE is MIT CloudNimble 2026. |
| Package ID | **XmlEssentials** | CloudNimble is **not** in Essentials package IDs (ClaudeEssentials, BlazorEssentials, McpEssentials.Core). |
| Assembly name | **`CloudNimble.XmlEssentials`** | Product assembly. Does **not** have to rhyme with STJ. |
| Root namespace | **`CloudNimble.Text.Xml`** | STJ parallel. **Not** `System.Text.Xml`. |
| Project name | `CloudNimble.XmlEssentials` | Matches assembly. |
| Generator project | `CloudNimble.XmlEssentials.SourceGeneration` | Packed **inside** the `XmlEssentials` nupkg as an analyzer. |
| Authors / Company | CloudNimble, Inc. | |
| `Directory.Build.props` PackageId | Strip `CloudNimble.` → **`XmlEssentials`** | `CloudNimble.XmlEssentials` packs as `XmlEssentials` with the usual Essentials rule. No special-case override. |
| RootNamespace | `CloudNimble.Text.Xml` | Set explicitly; do not default to assembly name. |
| EasyAF | May `PackageReference` later | Better Together. Not this repo. |
| ModernXmlSerializer | Abandoned | Mention only as prior art / what not to copy. |

Install story:

```bash
dotnet add package XmlEssentials
```

```csharp
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

string xml = XmlSerializer.Serialize(person);
Person? p = XmlSerializer.Deserialize<Person>(xml);
```

Name collision with `System.Xml.Serialization.XmlSerializer` is **accepted**. This library does not import BCL XML. Consumers who only `using CloudNimble.Text.Xml` get our type. SDK implicit usings do **not** include `System.Xml.Serialization`. CS0104 happens only when both namespaces are imported in the same file — the same situation as `Newtonsoft.Json.JsonSerializer` vs `System.Text.Json.JsonSerializer`.

The same collision policy applies to `XmlDocument`, `XmlElement`, `XmlException`, `XmlAttribute`, `XmlNode`. Ours live in `CloudNimble.Text.Xml`. BCL types are not referenced.

**Do not use** package id `CloudNimble.XmlEssentials` (the NuGet id is `XmlEssentials`) or namespace `CloudNimble.XmlEssentials` (the namespace is `CloudNimble.Text.Xml`). The assembly and projects **are** `CloudNimble.XmlEssentials`.

## Key decisions

| # | Decision | Rationale |
| --- | --- | --- |
| K1 | High-level type is `XmlSerializer` (static), not `XmlDataSerializer` | STJ parallel. Collision with BCL is accepted. #125299 used `XmlData*` to dodge BCL; we are not the BCL. |
| K2 | Package `XmlEssentials`, assembly `CloudNimble.XmlEssentials`, namespace `CloudNimble.Text.Xml` | Package/assembly follow Essentials. Namespace is the STJ parallel and a `using` swap hedge. |
| K3 | Zero BCL XML, including internally | "BCL XML is dead" for this product. Own UTF-8 reader/writer. |
| K4 | Own attribute family in `CloudNimble.Text.Xml.Serialization` | Reusing BCL attributes forces `using System.Xml.Serialization` and CS0104, and is a BCL XML dependency on DTO files. |
| K5 | `[XmlInclude]` = STJ `[JsonInclude]` (non-public members). Known derived types = `[XmlDerivedType]`. | BCL `[XmlInclude]` means known types. Do not collide those semantics. |
| K6 | `[XmlRoot]`, `[XmlAttribute]`, `[XmlElement]`, `[XmlArray]`, `[XmlText]` in **our** namespace | XML needs element-vs-attribute. STJ does not. We add XML-shaped attributes on top of the STJ set, not instead of it. |
| K7 | Default mapping: public property → child element named by naming policy | STJ property → JSON property. XML analogue is child element, not attribute. |
| K8 | Collections default to **repeated elements with the property name** (STJ-like). `[XmlArray]`/`[XmlArrayItem]` opt into a wrapper. | STJ has no wrapper. Most "modern" XML APIs still need wrappers; they are opt-in, not default. |
| K9 | Default member order is most-base then most-derived, declaration order within each type. `[XmlPropertyOrder]` may **interleave** base and derived. That interleave is the break from BCL XSD extension (Order is per-class; derived may only append). | Matches the mapping section. Default walk is XSD-shaped; opt-in Order is why this library exists (ModernXmlSerializer). |
| K10 | Mixed content: reader/writer/DOM yes. Serializer mapping via `[XmlMixed]` content lists (ordered nodes / DTOs). Types without `[XmlMixed]` skip or disallow unexpected mixed input per [XML fidelity](02-xml-fidelity.md#mixed-content). | XML mixed content has no JSON analogue; `[XmlMixed]` is the mapper for it. |
| K11 | Simple content (attributes + `[XmlText]`) is supported | `<Price currency="USD">19.99</Price>` is common and fits the property model. |
| K12 | `xsi:nil` is first-class (`WriteNil`, nillable properties), implemented by this stack | JSON `null`. We do not reference `System.Xml`. |
| K13 | No DTD → XXE closed | Presence of `<!DOCTYPE` (or other `<!` markup besides comments) is `XmlException`. |
| K14 | UTF-8 only. Other encodings declared in the XML declaration fail clearly. Reader **accepts** an optional BOM. Writer **`EmitByteOrderMark` defaults to `false`**. When `true`, the writer emits `EF BB BF` once at the start of the document (before the declaration). | STJ does not BOM UTF-8 by default. BOM is a file/sniffer interop knob (`OmitXmlDeclaration` + Windows/Excel/EDI). |
| K15 | Lexical forms are XSD 1.0 datatypes, invariant culture, with `whiteSpace = collapse` applied first; one implementation (`XmlLexical`) serves reader, DOM, and serializer. **`decimal` read and write reject exponent** (`1E+2` is `float`/`double`, not XSD decimal). | `boolean` true/false/1/0; numbers without group separators; `xs:dateTime` / `xs:date` / `xs:time` / `xs:duration`; `xs:base64Binary`. |
| K16 | `Dictionary<TKey,TValue>` / `IDictionary<,>` map as STJ objects do. Default: string keys that are valid XML names after `DictionaryKeyPolicy` become child element names; otherwise repeating `<Key>`/`<Value>` entries. `[XmlExtensionData]` and `[XmlNamespaceDeclarations]` remain bags, not maps. | JSON object analogue. |
| K17 | Source generator packed inside `XmlEssentials` nupkg | STJ-style. No extra `PrivateAssets` package. |
| K18 | Reflection gated by `XmlSerializer.IsReflectionEnabledByDefault`; context overloads have no AOT warning | Match STJ. |
| K19 | Object `DeserializeAsync(Stream)` is buffer-then-parse (STJ). `DeserializeAsyncEnumerable` / `SerializeAsyncEnumerable` stream items (including `topLevelValues`). Reader `IsFinalBlock` resume is required. Public `XmlReaderState` is STJ-shaped (`Options` only); **implementation must snapshot** element stack, namespace scopes, partial token, and positions — see [04](04-api-reader.md#xmlreaderstate-implementation-contract). | Match STJ async shapes. |
| K20 | PackageId = `$(MSBuildProjectName.Replace('CloudNimble.', ''))` on the primary library → `XmlEssentials` | Assembly `CloudNimble.XmlEssentials` already strips to the right package id. |
| K21 | **100% line** on `CloudNimble.XmlEssentials.dll` is the CI **fail** gate. **Branch** is reported (ratchet 80%; tokenizer may be branch-excluded with a list — never from line). **Host is MTP** (`global.json` `test.runner: Microsoft.Testing.Platform`). Collection: `Microsoft.Testing.Extensions.CodeCoverage` via `dotnet test --coverage --coverage-output-format cobertura --coverage-output artifacts/coverage/coverage.cobertura.xml`. That extension **does not fail** on line 100%. Fail gate: `dotnet run src/coverage-gate.cs -- artifacts/coverage/coverage.cobertura.xml`. **Not** `coverlet.runsettings` / VSTest DataCollector. Ratchet starts in [PR 2](22-pr-plan.md). | MTP ignores coverlet DataCollectors. 100% branch on a tokenizer is how gates get `[ExcludeFromCodeCoverage]`'d. |
| K22 | Benchmarks cache BCL `XmlSerializer` instances. Label BCL column “BCL defaults (wrapped collections)” unless BCL attributes pin the infoset. STJ is a sidecar (XML tax vs JSON), not a win/loss. | ModernXmlSerializer's benchmark was dishonest. Mapping mismatch is fairness, not a bug. |
| K23 | TFMs `net10.0;net11.0` | Match ClaudeEssentials. |
| K24 | Comments: serializer skips by default (`XmlCommentHandling.Skip`); reader default Skip | Match STJ `JsonCommentHandling.Skip` as the useful default. Reader options can Disallow/Allow. |
| K25 | Root element name = type name via naming policy, overridable `[XmlRoot]` in our namespace. Primitive roots use the CLR name (`Int32`, not `int`). | Not `[XmlSerializableRoot]`. |
| K26 | Element name comparison is `{local name, namespace URI}`, never prefix | Prefix is serialization sugar. |
| K27 | Writer writes `<e/>` from `WriteEndElement()` when an element has no content; `WriteFullEndElement()` writes `<e></e>` | Match BCL `XmlWriter` usefully without taking a dependency. |
| K28 | Empty element with no `xsi:nil` deserializes as empty string / default struct / empty collection, **not** null | `xsi:nil="true"` is null. This distinction is the XML data model STJ does not have. |
| K29 | Case-sensitive names by default; `PropertyNameCaseInsensitive` is an explicit interop option | XML 1.0 names are case-sensitive. |
| K30 | Generator supports **internal types in the consuming compilation** only (not friend assemblies unless those types are compiled in the same compilation). | Improvement over BCL `XmlSerializer`. Friends are not “in the compilation.” |
| K31 | `Serialize(null)` of a nillable reference type writes the root element with `xsi:nil="true"`. If `NilHandling` is `Omit`, throw `InvalidOperationException` (cannot omit the document). Value-type null is impossible. | STJ writes JSON `null`; XML analogue is a nillable root. |
| K32 | On every max-* **except** `MaxDocumentByteCount`, `0` means “apply the default from the DoS table.” `MaxDocumentByteCount` `0` stays unbounded. Stream/document buffering **enforces** the cap while growing, not after. | `default(XmlReaderOptions)` is all zeros; 0-byte comments would otherwise reject every document. |
| K33 | `XmlNumberHandling` is `[Flags]` with `Strict = 0` and `AllowNamedFloatingPointLiterals = 1` only. Options default is `AllowNamedFloatingPointLiterals` (XSD INF/NaN). No `WriteAsString` / `AllowReadingFromString`. | JSON-string numbers have no XML analogue. |
| K34 | No `EncodeInvalidCharsAsHex` public API. Char legality follows the document version (1.0 or 1.1). | [XML 1.0 and 1.1](02-xml-fidelity.md#xml-10-and-11). |
| K46 | XML **1.0 and 1.1**. One tokenizer. Reader version comes from the declaration (omitted ⇒ 1.0). Writer/serializer `XmlVersion` defaults to 1.0. Not two reader types. 1.1 is not a superset of 1.0 (NEL, RestrictedChar, `xmlns:p=""`). | BCL `XmlReader`/`XmlWriter`/`XmlSerializer` never implemented 1.1. |
| K47 | XSD support is **compile-time codegen**, not runtime `XmlSchemaSet`. `AdditionalFiles` `*.xsd` → POCOs + our attributes + `[XmlSerializable]` entries; the existing serializer generator then emits AOT ser/deser. No schema load, no `XmlUrlResolver`, no DTD, at runtime. | Same AOT story as the type generator. Runtime XSD validation is a different product. |
| K35 | Multiple `[XmlElement]` / `[XmlArrayItem]` on one member = per-member polymorphic choice (`Type` + `ElementName`). `Type` required on every extra. Complements type-level `[XmlDerivedType]`. | BCL choice pattern; otherwise `AllowMultiple = true` is unspecified. |
| K36 | `[XmlPropertyOrder]` / `XmlElement.Order` / `XmlArray.Order`: unset is **`-1`**, not `0`. Members with explicit order (`>= 0`) sort first by order (stable). Members with unset order follow in flattened declaration order. | BCL uses `-1` for unset; default `0` would shove unspecified members to the front. |
| K37 | Missing `required` / `[XmlRequired]` / required constructor parameter on deserialize → `XmlException`. `RespectRequiredConstructorParameters` default **true**. | STJ throws; XML must too. |
| K38 | Serializer `WhitespaceHandling` is forwarded to the `Utf8XmlReader` used on deserialize only. Serialize never preserves significant whitespace (indent is cosmetic). | Options property is not a mapper mixed-content feature. |
| K39 | Test package id is **`Breakdance.Assemblies` 8.*** (not `CloudNimble.Breakdance.Assemblies`). Local Breakdance repo is 9.0.0-preview; 8.* matches EasyAF. | ClaudeEssentials/EasyAF strip `CloudNimble.` from Breakdance project names too. |
| K40 | Public XML comments from the PR that introduces each public type. PR 1 sets `TreatWarningsAsErrors` and `NoWarn` **CS1591** on projects that do not yet have public API. Remove CS1591 from `NoWarn` for the runtime library in the same PR that adds its first public type (PR 2). | Otherwise PRs 2–9 cannot compile under `GenerateDocumentationFile`. |
| K41 | Source generator ships **both** `XmlSourceGenerationMode.Serialization` and `Metadata`. `Default` = `Metadata \| Serialization` (STJ). | Same split as STJ. |
| K42 | Source-gen interceptors rewrite `XmlSerializer.Serialize`/`Deserialize` call sites to generated type-info overloads when a matching context exists. | Same as STJ interceptors. |
| K43 | `ReferenceHandler` (`Preserve` / `IgnoreCycles`). Preserve uses attributes in namespace `urn:CloudNimble.Text.Xml:refs` (`id`, `ref`), not SOAP `href`. | STJ `$id`/`$ref` analogue. |
| K44 | `PipeReader` / `PipeWriter` overloads match STJ. | Same I/O surfaces. |
| K45 | `XmlSerializerOptions.Default`, `.Web`, and `.Strict`. `PascalCase` naming policy. `DictionaryKeyPolicy`, `AllowDuplicateProperties`, `AllowOutOfOrderMetadataProperties`, `GetConverter`, `TryGetTypeInfo` / `TryGetTypeInfo<T>`. | Same options surface as STJ. |

---

[Index](README.md) · [Previous: Overview](00-overview.md) · [Next: XML fidelity](02-xml-fidelity.md)
