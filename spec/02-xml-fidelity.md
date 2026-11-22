# XML fidelity

Part of the [XmlEssentials spec](README.md).

JSON is simple; XML is not. STJ shipped by cutting Newtonsoft's long tail. This section is the cut.

Normative citations: [XML 1.0 Fifth Edition](https://www.w3.org/TR/xml/), [XML 1.1 Second Edition](https://www.w3.org/TR/xml11/), [Namespaces in XML 1.0](https://www.w3.org/TR/xml-names/), [Namespaces in XML 1.1](https://www.w3.org/TR/xml-names11/). Implement those productions; do not invent a “Letter | NameChar” shorthand.

## XML 1.0 and 1.1

One tokenizer. A version flag. Not two reader types. Not a serializer type discriminator.

1.1 is **not** a superset of 1.0. NEL (`U+0085`) is ordinary text in 1.0 and a line ending in 1.1 (becomes `#xA`). Applying 1.1 EOL rules to a 1.0 document changes the infoset.

| | 1.0 | 1.1 |
| --- | --- | --- |
| How selected | `version="1.0"` or omitted (omitted ⇒ 1.0) | `version="1.1"` |
| Read | Declaration sets `Utf8XmlReader.Version` | Same |
| Write | `XmlWriterOptions.Version` / `XmlSerializerOptions.XmlVersion`, default `Version10` | `Version11` emits `version="1.1"` |
| Namespaces | NS 1.0. `xmlns=""` unbinds default. `xmlns:p=""` is an error. | NS 1.1. `xmlns:p=""` **unbinds** prefix `p`. Namespace names are IRIs. |
| Line endings | `#xD#xA` and `#xD` → `#xA` | Also `#x85` (NEL) and `#x2028` (LS) → `#xA` |
| Char | 1.0 Char. C0 other than TAB/LF/CR illegal as literals **and** NCRs. | All Unicode except `NUL`. **RestrictedChar** (remaining C0; C1 except NEL) **must** be numeric character references, never literals. Writer emits RestrictedChar as `&#xHH;`. |
| Names | XML 1.0 5th ed `NameStartChar` / `NameChar` | XML 1.1 Name productions (superset of 1.0 5th ed) |

`XmlVersion` enum: [reader](04-api-reader.md#enums).

## Supported (reader / writer / DOM)

- **UTF-8 only.** Optional BOM (`EF BB BF`) is accepted and consumed on **read**. If the XML declaration names an encoding, it must be `utf-8` / `UTF-8` / `utf8` (ASCII-case-insensitive). Anything else is `XmlException` with a message that names the declared encoding and states that XmlEssentials is UTF-8 only.
- **Writer BOM:** `XmlWriterOptions.EmitByteOrderMark` / `XmlSerializerOptions.EmitByteOrderMark`, default **`false`**. When `true`, emit exactly `EF BB BF` once at byte 0, before the XML declaration (or before the root if the declaration is omitted). Do not emit a BOM in the middle of a document or per fragment when `AllowMultipleRoots` is in play on the reader — writer still one BOM per `Utf8XmlWriter` lifetime, at first output.
- **Writer declaration** (when not omitted): `<?xml version="1.0" encoding="utf-8"?>` or `version="1.1"` when `XmlVersion.Version11` (lowercase `utf-8`). If `XmlWriterOptions.Standalone` is `true`/`false`, append ` standalone="yes"` / ` standalone="no"` before `?>`. No other attributes.
- Well-formed **XML 1.0 or 1.1** documents and fragments (version as above):
  - Elements (prefixed and unprefixed), empty (`<e/>`) and start+end (`<e></e>`).
  - Attributes.
  - Text.
  - CDATA.
  - Comments.
  - Processing instructions.
  - XML declaration as above.
- **Namespaces:** prefix, default `xmlns`, namespace URI on every name. `xmlns` and `xmlns:prefix` update the reader namespace stack. The `xml` prefix is bound to `http://www.w3.org/XML/1998/namespace`. The `xmlns` prefix is bound to `http://www.w3.org/2000/xmlns/`. **`xmlns=""` unbinds** the default namespace. **`xmlns:p=""`** unbinds prefix `p` **only in 1.1**; in 1.0 it is `XmlException`. Prefix rebinding in nested scopes is allowed; inner declaration hides outer until the element ends.
- Character entities for the five predefined entities (`&lt;` `&gt;` `&amp;` `&quot;` `&apos;`) and numeric character references (`&#NNNN;`, `&#xHHHH;`).
- `xsi:nil` as a first-class serializer concept (see [object mapping](09-object-mapping.md)). The writer emits `xmlns:xsi` and `xsi:nil="true"` itself. Constant URI: `http://www.w3.org/2001/XMLSchema-instance`.

## Infoset we expose

After parse, the infoset the rest of the stack sees is:

- Text and attribute values **after** EOL normalization and (for attributes) attribute-value normalization.
- Names as `{local, namespace URI}`; **prefixes are not significant** for equality or mapping.
- Empty element vs start+end is visible (`IsEmptyElement`).
- Comments/PIs as tokens/nodes when not skipped.

`LineNumber` is 1-based after EOL normalization. `LinePosition` is 1-based **UTF-8 byte** offset in the current line (STJ-ism, not xmlconf character columns). Document this in tests; do not fail xmlconf on column units.

## End-of-line handling

Before any other parse of a text/attribute/comment/PI/CDATA payload, on the decoded UTF-8 code-point stream:

- **1.0** (§2.11): replace `#xD#xA` and remaining `#xD` with `#xA`.
- **1.1** (§2.11): also replace `#x85` and `#x2028` with `#xA`.

This changes text values, line numbers, and `LinePosition`.

## Attribute-value normalization (XML 1.0 §3.3.3)

No DTD ⇒ every attribute is treated as CDATA:

1. EOL normalize as above.
2. Replace `#x9`, `#xA`, `#xD` with `#x20`.
3. Expand the five predefined entities and numeric character references.
4. Writer **always** double-quotes attribute values.

## Names

Use the `NameStartChar` / `NameChar` productions of the **document version**. Both 1.0 5th ed and 1.1 2nd ed list explicit code-point ranges (including `#x10000–#xEFFFF`). Do **not** implement “Letter | '_' | ':'” as a Unicode category heuristic.

## Mixed content

```xml
<p>hello <b>world</b></p>
```

| Layer | Behavior |
| --- | --- |
| `Utf8XmlReader` | Yes. Sequence of `Text`, `StartElement`, `Text`, `EndElement`. |
| `Utf8XmlWriter` | Yes. Callers write text and elements in any order. |
| DOM (`XmlDocument` / `XmlNode`) | Yes. Element children is an ordered list of nodes (text, element, comment, PI, CDATA). |
| `XmlSerializer` object **mapping** | **Yes.** `[XmlMixed]` on a `List<XmlNode>` / `List<object>` / content DTO list. See [object mapping](09-object-mapping.md#mixed-content). |
| Types **without** `[XmlMixed]` | If the type has `[XmlText]` **and** child-element members: `NotSupportedException` at type-info build (use `[XmlMixed]`). If the type has no `[XmlText]` and the payload has both text and child elements: unmapped-member handling. |

Simple content:

```xml
<Price currency="USD">19.99</Price>
```

maps to `[XmlAttribute] string Currency` + `[XmlText] decimal Amount`.

## Whitespace

| Layer | Default | Option |
| --- | --- | --- |
| Reader | Skip **insignificant** whitespace (CharData that is only `Space`, `Tab`, `LF`, `CR` and is not a child of an element that currently has mixed/simple text being accumulated as a value) | `XmlReaderOptions.WhitespaceHandling = XmlWhitespaceHandling.None \| Significant \| Insignificant` |
| Writer | No extra whitespace unless `Indented` | `IndentCharacter`, `IndentSize`, `NewLine` |
| Serializer | Does not preserve significant whitespace on **write**. Indent is a write-only cosmetic. On **read**, `XmlSerializerOptions.WhitespaceHandling` is forwarded to the `Utf8XmlReader` only ([K38](01-identity-and-decisions.md#key-decisions)). | `WriteIndented` |

`XmlWhitespaceHandling`:

- `Insignificant` (default): whitespace-only text between elements is not reported as tokens. Whitespace inside a leaf element's value is **significant** and is part of the value (`<n> a </n>` is `" a "`).
- `Significant`: every whitespace-only text node is reported as `XmlTokenType.Text`.
- `None`: reject whitespace-only text between elements (`XmlException`). Rare; available for strict payload contracts.

## Rejected constructs (errors, not silent skip)

| Construct | Behavior |
| --- | --- |
| `<!DOCTYPE` | `XmlException` (XXE closed). |
| `<!ENTITY`, `<!ELEMENT`, `<!ATTLIST`, `<!NOTATION`, any other `<!` that is not `<!--` | `XmlException`. |
| Undefined general entity `&foo;` | `XmlException`. Only `&lt;` `&gt;` `&amp;` `&quot;` `&apos;` and numeric refs. |
| Non-UTF-8 encoding in declaration | `XmlException`. |
| `NUL` (`#x0`) | `XmlException` in both versions. |
| 1.0: C0 other than TAB/LF/CR (literal or NCR) | `XmlException`. |
| 1.1: RestrictedChar as a **literal** | `XmlException`. Same code point as an NCR is legal; the writer emits the NCR. |
| 1.0 document with `xmlns:p=""` | `XmlException`. |
| Duplicate attribute on one element (well-formedness constraint) | `XmlException`. |
| Undeclared prefix | `XmlException`. |
| `]]>` in text (must be escaped as `]]&gt;`) | Reader accepts the escaped form; writer escapes. Raw `]]>` in text is a well-formedness error on write (or escaped). |
| CDATA containing `]]>` | Writer throws; it does not silently split CDATA. |

## Well-formedness constraints the reader enforces

1. Single root element unless `AllowMultipleRoots` (fragments).
2. Start/end tag local name + namespace URI match (prefix need not match).
3. Attribute names unique per element by `{local, ns}` (`xmlns` / `xmlns:prefix` included).
4. No `<` in attribute values (must be `&lt;`).
5. Element and attribute names match the document version's `NameStartChar` / `NameChar`.
6. Comments do not contain `--`.
7. PI target is not `xml` (case-insensitive) except for the XML declaration handled separately.
8. Depth ≤ `MaxDepth`.
9. Attribute count ≤ `MaxAttributeCount` (**including** `xmlns` and `xmlns:prefix`).
10. Name byte length ≤ `MaxNameByteCount`.

## DoS limits (defaults)

| Limit | Default | Applied on | `0` means |
| --- | --- | --- | --- |
| `MaxDepth` | 64 | Reader, writer, serializer, document | apply default |
| `MaxAttributeCount` | 1000 (includes xmlns*) | Reader, **writer** (`XmlWriterOptions`), serializer, document | apply default |
| `MaxNameByteCount` | 1024 | Reader, **writer** (`XmlWriterOptions`), serializer, document | apply default |
| `MaxCommentByteCount` | 1_048_576 | Reader, serializer, document — **not writer** (caller-supplied strings) | apply default |
| `MaxTextByteCount` | 16_777_216 | Reader, serializer, document (per text/CDATA node) — **not writer** | apply default |
| `MaxDocumentByteCount` | 0 (unbounded) | `XmlDocument.Parse`, stream deserialize, serializer async | **unbounded** |
| `DefaultBufferSize` | 16_384 | Stream async / buffer growth | apply default |

`default(XmlReaderOptions)` / `default(XmlWriterOptions)` is all zeros. **Every max-* except `MaxDocumentByteCount` treats `0` as “use the Default column.”** `MaxDocumentByteCount == 0` stays unbounded. When a cap is set, enforce it **while** `ArrayBufferWriter` grows, not after. Exceeding a limit is `XmlException` with the limit named in the message.

## Lexical rules (XSD 1.0, invariant)

Typed getters on [`Utf8XmlReader`](04-api-reader.md) and serializer primitives use these forms. Current token must be Text, CData, or Attribute (or Comment for `GetComment`). Throw `XmlException` on mismatch or invalid lexical form.

| Type | Write | Read accepts |
| --- | --- | --- |
| `bool` | `true` / `false` | `true`, `false`, `1`, `0` (XSD boolean). Leading/trailing XML whitespace trimmed. |
| integers | invariant decimal, optional `-` | same; reject `+`, group separators, hex, exponent |
| `float`/`double` | round-trip (`G9`/`G17`) or `INF`/`-INF`/`NaN` | XSD: number, `INF`, `-INF`, `NaN`; also `inf`/`nan` case-insensitive as a documented extension. `XmlNumberHandling.Strict` rejects named literals. Options default is `AllowNamedFloatingPointLiterals`. |
| `decimal` | invariant, **no exponent** | XSD 1.0 `decimal` only: optional sign, digits, optional fraction. **Reject exponent** (`1E+2` / `1e2` is `XmlException`). Not a documented extension. |
| `DateTime` | `xs:dateTime` without offset if `DateTimeKind.Unspecified`; `Z` if Utc; offset if Local | `xs:dateTime` |
| `DateTimeOffset` | `xs:dateTime` with offset or `Z` | `xs:dateTime` with offset or `Z` |
| `DateOnly` | `xs:date` `yyyy-MM-dd` | `xs:date` |
| `TimeOnly` | `xs:time` | `xs:time` |
| `TimeSpan` | `xs:duration` (`PT1H2M3S`, …) | `xs:duration` |
| `Guid` | .NET `"D"` (hyphens) | `"D"`, `"N"`, `"B"`, `"P"` |
| `byte[]` | `xs:base64Binary` | XSD base64 |

See [serializer enums](07-api-serializer.md#enums).

---

[Index](README.md) · [Previous: Identity and decisions](01-identity-and-decisions.md) · [Next: Architecture](03-architecture.md)
