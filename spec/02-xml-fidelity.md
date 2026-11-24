# XML fidelity

Part of the [XmlEssentials spec](README.md).

JSON is simple; XML is not. STJ shipped by cutting Newtonsoft's long tail. This section is the cut.

Normative citations: [XML 1.0 Fifth Edition](https://www.w3.org/TR/xml/), [XML 1.1 Second Edition](https://www.w3.org/TR/xml11/), [Namespaces in XML 1.0](https://www.w3.org/TR/xml-names/), [Namespaces in XML 1.1](https://www.w3.org/TR/xml-names11/). Implement those productions; do not invent a “Letter | NameChar” shorthand.

## Authority

1. **W3C XML.** XML 1.0 Fifth Edition, XML 1.1 Second Edition, Namespaces in XML 1.0 and 1.1, XML Schema Part 2 (datatypes). These win.
2. **BCL in .NET 11.** `System.Xml.XmlReader`, `XmlWriter`, and `XmlException` are the living record of how .NET developers expect XML issues to be handled when the W3C documents are silent (limits, option names, UTF-8-only stacks, how an error is reported to the caller).
3. **This spec.** It records the subset we implement and every deliberate departure. When it disagrees with (1), it is wrong and is corrected here. A departure from (1) or (2) is listed under [Departures](#departures) with the reason.

Recorded corrections (so they are not silently reversed):

- **End tags repeat the start tag's Name, prefix included.** An earlier draft of this page said prefixes were insignificant for start/end matching. That contradicted [XML 1.0 §3 WFC: Element Type Match](https://www.w3.org/TR/xml/#GIMatch). Namespaces in XML does not relax it. `<a:e xmlns:a="urn:u" xmlns:b="urn:u"></b:e>` is an error. Infoset equality of names is still `{local, namespace URI}`; the prefix rule is about the document text.
- **A well-formedness error inside a token that has not finished in a non-final buffer is reported when that token can be completed.** `NeedMore` comes first. Example: `<a>&bogus;` split before `;` does not throw "Undefined entity." until the rest of the token arrives. XML does not define streaming buffers; this matches how a non-final `Utf8JsonReader` defers errors, and it is the only behavior that keeps split reads consistent.

## XML 1.0 and 1.1

One tokenizer. A version flag. Not two reader types. Not a serializer type discriminator.

1.1 is **not** a superset of 1.0. NEL (`U+0085`) is ordinary text in 1.0 and a line ending in 1.1 (becomes `#xA`). Applying 1.1 EOL rules to a 1.0 document changes the infoset.

| | 1.0 | 1.1 |
| --- | --- | --- |
| How selected | `version="1.0"` or omitted (omitted ⇒ 1.0) | `version="1.1"` |
| Read | Declaration sets `Utf8XmlReader.Version` | Same |
| Write | `XmlWriterOptions.Version` / `XmlSerializerOptions.XmlVersion`, default `Version10` | `Version11` emits `version="1.1"` |
| Namespaces | NS 1.0. `xmlns=""` unbinds default. `xmlns:p=""` is an error. | NS 1.1. `xmlns:p=""` **unbinds** prefix `p`. Namespace names are IRIs. |
| Line endings | `#xD#xA` and `#xD` → `#xA` | Also `#xD#x85` (one line end), `#x85` (NEL) and `#x2028` (LS) → `#xA` |
| Char | 1.0 Char. C0 other than TAB/LF/CR illegal as literals **and** NCRs. | All Unicode except `NUL`. **RestrictedChar** (remaining C0; C1 except NEL) **must** be numeric character references, never literals. Writer emits RestrictedChar as `&#xHH;`. |
| Names | XML 1.0 5th ed `NameStartChar` / `NameChar` | XML 1.1 Name productions (superset of 1.0 5th ed) |

`XmlVersion` enum: [reader](04-api-reader.md#enums).

## Supported (reader / writer / DOM)

- **UTF-8 only.** Optional BOM (`EF BB BF`) is accepted and consumed on **read**. If the XML declaration names an encoding, it must be `utf-8` / `UTF-8` / `utf8` (ASCII-case-insensitive). Anything else is `XmlException` with a message that names the declared encoding and states that XmlEssentials is UTF-8 only. That check is for bytes: a document given as a `string` or a span of characters has no encoding left to declare, so the name in its declaration is not checked (`System.Xml` writes `encoding="utf-16"` to a `StringWriter` and reads it back). Bytes that begin as UTF-16 or UTF-32, with or without a byte order mark, are `XmlException` "XmlEssentials is UTF-8 only. The input is UTF-16 or UTF-32."
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
- Names as `{local, namespace URI}`; **prefixes are not significant** for equality or mapping. (They are significant in one place: an end tag must repeat the prefix of its start tag, which is a well-formedness rule about the document text, not about names.)
- Empty element vs start+end is visible (`IsEmptyElement`).
- Comments/PIs as tokens/nodes when not skipped.

`LineNumber` is 1-based after EOL normalization. `LinePosition` is 1-based **UTF-8 byte** offset in the current line (STJ-ism, not xmlconf character columns). Document this in tests; do not fail xmlconf on column units.

## End-of-line handling

Before any other parse of a text/attribute/comment/PI/CDATA payload, on the decoded UTF-8 code-point stream:

- **1.0** (§2.11): replace `#xD#xA` and remaining `#xD` with `#xA`.
- **1.1** (§2.11): also replace `#xD#x85` (the pair is **one** line end), `#x85`, and `#x2028` with `#xA`. Text that holds nothing but these and other white space is white space: the replacement comes before `WhitespaceHandling` is applied.

This changes text values, line numbers, and `LinePosition`.

## Attribute-value normalization (XML 1.0 §3.3.3)

No DTD ⇒ every attribute is treated as CDATA:

1. EOL normalize as above.
2. Replace `#x9`, `#xA`, `#xD` with `#x20`.
3. Expand the five predefined entities and numeric character references.
4. Writer **always** double-quotes attribute values.

## Round-trip guarantees

Reading normalizes; writing must therefore protect anything that normalization would change. These are requirements, tested as properties (`XmlRoundTripTests`), not conventions.

**Writer → reader.** Any string the writer accepts as a value is returned unchanged by the reader:

| Value contains | In element text | In an attribute value | Why |
| --- | --- | --- | --- |
| `&`, `<` | `&amp;`, `&lt;` | `&amp;`, `&lt;` | Markup. |
| `"` | literal | `&quot;` | Attribute values are double-quoted. |
| `>` after `]]` | `&gt;` | literal | `]]>` may not appear in character data (§2.4). |
| CR (`#xD`) | `&#xD;` | `&#xD;` | A literal CR is read as LF (§2.11). |
| TAB | literal | `&#x9;` | A literal TAB in an attribute is read as a space (§3.3.3). |
| LF | the line ending `NewLine` asks for: LF, or CR LF, which is read as LF (§2.11) | `&#xA;` | A literal LF in an attribute is read as a space (§3.3.3). |
| NEL (`#x85`), LS (`#x2028`) in **1.1** | `&#x85;`, `&#x2028;` | same | Literals are read as LF in 1.1. In 1.0 they are ordinary characters and stay literal. |
| 1.1 RestrictedChar | `&#xN;` | `&#xN;` | Must be a reference (§2.2). |
| A character outside `Char` for the version | throws | throws | Cannot be represented. |

**Predictable normalization.** What is read and written again keeps its infoset and the layout between its nodes. Everything else that is rewritten is rewritten by a named setting of the writer, which applies to every element in the same way, whichever code asked for the element: a caller of the writer, the serializer, a generated context, a document or a node. So a document changes only in the ways the options name, and a document that is already in the form the options ask for comes back byte for byte.

| Setting (`XmlWriterOptions`, and the same name on `XmlSerializerOptions`) | What it decides | Values |
| --- | --- | --- |
| `EmptyElementStyle` | How an element with no content is written. | `SelfClosing` `<e/>` (default), `SelfClosingWithSpace` `<e />` (the form the writers in `System.Xml` use), `StartAndEndTag` `<e></e>`. |
| `AttributeStyle`, `AttributesPerLine` | How the attributes of a start tag are laid out. | `Inline` (default): all on the line of the tag. `PerLine`: `AttributesPerLine` to a line (1 unless set), each later line indented so its first attribute is under the first attribute of the tag. |
| `NewLine` | Every line break the writer writes: indentation, the breaks `PerLine` adds, and the line breaks in text, CDATA sections, comments and processing instructions. | `"\n"` or `"\r\n"`; the default is `Environment.NewLine`. These are the values `JsonWriterOptions.NewLine` accepts, and as there the value is never null: `default(XmlWriterOptions)` gives `Environment.NewLine` too. |
| `OmitXmlDeclaration` | Whether a declaration is written. | The writer writes one unless this is set, whether or not the source had one. |

Rules that follow from this:

- An element that was given an empty string has content, a value of no characters, and is written `<e></e>` under every value of `EmptyElementStyle`. That keeps it apart from an element given nothing and from one that is nil.
- `EmptyElementStyle` applies to an element ended with `WriteEndElement` before any content, to `WriteEmptyElement`, and to `WriteElementString` with a null value. `WriteFullEndElement` always writes an end tag. A document and a node end an element that was read with an end tag with `WriteFullEndElement`, so `<e></e>` stays `<e></e>` under every value, and an element read as one tag takes the form the setting asks for.
- `PerLine`: a line after the first starts with the white space that starts the line of the tag, and then one space for each character of the tag up to its first attribute (`<`, the name with its prefix, and the space). It does not depend on `Indented`. A namespace declaration and `xsi:nil` are attributes and are counted. An element that does not start its line follows the same rule.
- `NewLine` changes no value. A reader gives CR LF back as LF (XML 1.0 and 1.1 §2.11), a CR that is part of a value is written as `&#xD;`, and a line break in an attribute value is written as `&#xA;`. In a comment, a CDATA section or a processing instruction, an LF that follows a CR is left as it is. Any value but the two is rejected with `ArgumentOutOfRangeException`, because nothing else is read back as a line feed. `XmlElement.GetRawXml()` and `XmlNode.ToString()` take no options and always give LF.

**Reader → DOM → writer.** `XmlDocument.WriteTo` reproduces the infoset it parsed, and for a document already in the form the writer's options ask for it reproduces the bytes:

- Namespace declarations are written **as parsed**, in their original position among the attributes. They are not regenerated from the names in use, because a prefix may be referenced only from content (`xsi:type="p:T"`, XPath, QName-valued attributes).
- `xmlns=""` is preserved, and is added when a sub-tree in no namespace is written under a default namespace. Without it the element would change namespace.
- Attribute order is preserved.
- `<e/>` and `<e></e>` are preserved (`IsEmptyElement`).
- Comments and processing instructions before and after the root are preserved.
- Not preserved, because they are not in the infoset: the quote character (always `"`), whitespace inside tags (the layout of attributes follows `AttributeStyle`), the form of an empty-element tag (`<e/>` or `<e />`, which follows `EmptyElementStyle`), the line ending (which follows `NewLine`), whitespace outside the root, the spelling of a character reference, and the declaration's spelling (it is rewritten as `version` + `encoding="utf-8"` + `standalone`).

The mutable tree (`XmlNode`) keeps namespace declarations and attribute order the same way, and `XmlElementNode.IsEmptyElement` records `<e/>` versus `<e></e>`: it is `false` for an element that was read with an end tag, `true` for an element that is created, can be set, and is not read while the element has children. `XmlNode.Parse` returns the root element, so the declaration and everything outside the root are not part of the tree.

**Indentation never changes content.** With `Indented`, whitespace is inserted only between elements. An element that holds character data is written on one line, including its child elements and its end tag, because whitespace next to character data is significant.

**Writer overloads and the default namespace.** `WriteStartElement(localName)` and the other overloads that take no namespace write the bare name and therefore inherit the default namespace in scope, as `System.Xml.XmlWriter` does for a null namespace. A `null` namespace argument on the `string` overloads means the same. An overload that is given a namespace writes the element in exactly that namespace: an empty string or an empty span means *no* namespace, and under a non-empty default namespace the writer adds `xmlns=""`, as `System.Xml.XmlWriter` does and as the DOM and node tree already did. With a prefix, the element keeps that prefix, and the prefix is declared on the element unless it is already bound to the namespace; another prefix for the same namespace does not count. Without a prefix, the writer uses the default namespace if it matches, then a prefix bound to the namespace that an inner element has not rebound, and otherwise declares the namespace as the default on the element.

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
| Reader and DOM | Report whitespace-only text between elements (`Significant`, the zero value). With `Insignificant`: skip **insignificant** whitespace (CharData that is only `Space`, `Tab`, `LF`, `CR` and is not a child of an element that currently has mixed/simple text being accumulated as a value) | `XmlReaderOptions.WhitespaceHandling = XmlWhitespaceHandling.None \| Significant \| Insignificant` |
| Writer | No extra whitespace unless `Indented` | `IndentCharacter`, `IndentSize`, `NewLine` |
| Serializer | Does not preserve significant whitespace on **write**. Indent is a write-only cosmetic. On **read**, `XmlSerializerOptions.WhitespaceHandling` is forwarded to the `Utf8XmlReader` only ([K38](01-identity-and-decisions.md#key-decisions)). | `WriteIndented` |

`XmlWhitespaceHandling`:

- `Insignificant` (the serializer default): whitespace-only text between elements is not reported as tokens. Whitespace inside a leaf element's value is **significant** and is part of the value (`<n> a </n>` is `" a "`).
- `Significant` (the reader and DOM default): every whitespace-only text node is reported as `XmlTokenType.Text`.
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
| XML declaration that does not match [production 23](https://www.w3.org/TR/xml/#NT-XMLDecl) | `XmlException`. `version` is required and first; then optional `encoding`, then optional `standalone` (`yes` or `no`); nothing else. `1.1` selects XML 1.1, any other `1.x` is read as 1.0, anything else is an error. Any non-ASCII byte, including NEL and LS, is an error ([XML 1.1 §2.11](https://www.w3.org/TR/xml11/#sec-line-ends)). |
| PI target `XML`, `Xml`, … | `XmlException`. Targets matching `xml` in any letter case are reserved; only lower-case `xml` at the start of the document is the declaration. |
| PI target containing `:` , or no whitespace between target and data | `XmlException` ([Namespaces §7](https://www.w3.org/TR/xml-names/#Conformance), [XML production 16](https://www.w3.org/TR/xml/#NT-PI)). |
| Name starting with `:`, ending with `:`, or with two colons | `XmlException`. Names are QNames ([Namespaces §4](https://www.w3.org/TR/xml-names/#ns-qualnames)). |
| Element name with the `xmlns` prefix | `XmlException` ([Namespaces §3](https://www.w3.org/TR/xml-names/#ns-decl)). Reader and writer. |
| A prefix other than `xml` bound to `http://www.w3.org/XML/1998/namespace`; any prefix bound to `http://www.w3.org/2000/xmlns/`; either one as the default namespace | `XmlException` (Namespaces §3). Reader and writer. |
| Writer: a character outside `Char` in a comment, CDATA section, or PI; or a 1.1 RestrictedChar there | `XmlException`. References are not recognized in those constructs, so the character cannot be written at all. |

## Well-formedness constraints the reader enforces

1. Single root element unless `AllowMultipleRoots` (fragments).
2. The Name in an end tag matches the start tag's, prefix included ([XML 1.0 §3](https://www.w3.org/TR/xml/#GIMatch), WFC: Element Type Match). `<a:e xmlns:a="urn:u" xmlns:b="urn:u"></b:e>` is an error although both prefixes are bound to one namespace.
3. Attribute names unique per element by `{local, ns}` (`xmlns` / `xmlns:prefix` included).
4. No `<` in attribute values (must be `&lt;`).
5. Element and attribute names match the document version's `NameStartChar` / `NameChar`.
    Names are QNames: at most one colon, never first or last. Element names do not use the `xmlns` prefix.
6. Comments do not contain `--`.
7. PI target is an NCName and is not `xml` in any letter case; lower-case `xml` first in the document is the XML declaration, which must match production 23.
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

Typed getters on [`Utf8XmlReader`](04-api-reader.md) and [`XmlElement`](06-api-dom.md), and serializer primitives, share **one** implementation (`XmlLexical`). The reader and the DOM never parse a value on their own, so they cannot disagree. On the reader the current token must be Text, CData, or Attribute (or Comment for `GetComment`). On `XmlElement`, an element's value is its concatenated text and CDATA content. Throw `XmlException` on mismatch or invalid lexical form.

Authority: [XML Schema Part 2: Datatypes, Second Edition](https://www.w3.org/TR/xmlschema-2/). Two layers apply to every value, and they are different specifications:

1. **XML** decides what the text is. Whitespace in element content is significant ([XML §2.10](https://www.w3.org/TR/xml/#sec-white-space)), so `<n> 42 </n>` has the text `" 42 "` and `GetString()` returns exactly that.
2. **XML Schema** decides what the text means as a typed value. Every datatype below has the `whiteSpace` facet fixed to `collapse` ([§4.3.6](https://www.w3.org/TR/xmlschema-2/#rf-whiteSpace)), so leading and trailing XML whitespace (space, TAB, LF, CR) is removed before parsing and `GetInt32()` on `<n> 42 </n>` returns `42`. None of these lexical spaces allows whitespace inside the value, except Base64.

| Type | XSD type | Write | Read accepts |
| --- | --- | --- | --- |
| `bool` | `boolean` | `true` / `false` | `true`, `false`, `1`, `0`. Case-sensitive. |
| `sbyte` `short` `int` `long` | `byte` `short` `int` `long` | invariant decimal, `-` when negative | `[+-]?[0-9]+`, in range. A leading `+` and leading zeros are legal. No group separators, hex, fraction, or exponent. |
| `byte` `ushort` `uint` `ulong` | `unsignedByte` … `unsignedLong` | invariant decimal | `[+]?[0-9]+`, in range. `-0` is zero; any other negative is out of range. |
| `float` / `double` | `float` / `double` | round-trip (`G9`/`G17`) or `INF` / `-INF` / `NaN` | `[+-]?` decimal mantissa, optional `[eE][+-]?[0-9]+`; or `INF`, `-INF`, `NaN`. **Documented extension:** the three literals in any letter case. `+INF` (XSD 1.1) and `Infinity` are rejected. `XmlNumberHandling.Strict` rejects the literals. |
| `decimal` | `decimal` | invariant, **no exponent** | `[+-]?` digits with an optional fraction (`12`, `12.5`, `.5`, `5.`). **Exponent is rejected** (`1E+2` is a `double`). |
| `DateTime` | `dateTime` | no zone if `Unspecified`; `Z` if `Utc`; offset if `Local` | `YYYY-MM-DDThh:mm:ss[.f+][Z\|(+\|-)hh:mm]`. No zone → `Unspecified`; `Z` or `+00:00` → `Utc`; other offset → converted to `Local`. |
| `DateTimeOffset` | `dateTime` | offset or `Z` | Same form. No zone takes the local offset at that instant (as `XmlConvert.ToDateTimeOffset` does). |
| `DateOnly` | `date` | `yyyy-MM-dd` | `YYYY-MM-DD` with an optional zone, which is validated and dropped. |
| `TimeOnly` | `time` | `xs:time` | `hh:mm:ss[.f+]` with an optional zone, which is validated and dropped. |
| `TimeSpan` | `duration` | `xs:duration` (`PT1H2M3S`, …): parts that are zero are left out, zero is `PT0S`, and a fraction of a second has up to seven digits with no trailing zeros, so every tick is written (`PT1.5S`, `PT0.0000001S`) | `[-]P[nD][T[nH][nM][n[.n]S]]`: at least one component, in that order, at least one after `T`, a fraction only on seconds. |
| `Guid` | (none) | .NET `"D"` | `"D"`, `"N"`, `"B"`, `"P"`. Not an XSD type. |
| `byte[]` | `base64Binary` | Base64 | Base64; whitespace between groups is allowed. Empty is an empty array. |

Rules that hold for all date and time types:

- Fields are fixed width. `2020-1-2` and `3:04:05` are rejected. `T` is upper case. A space is not a separator.
- `24:00:00` is legal ([§3.2.7.1](https://www.w3.org/TR/xmlschema-2/#dateTime-lexical-representation)) and means the first instant of the next day. `24:00:01` is not.
- Seconds are `00`–`59`. XSD 1.0 has no leap seconds.
- A zone is `Z` or `(+|-)hh:mm` up to `14:00`.
- Fraction digits past the seventh are dropped (100 ns resolution).

Deliberate limits, because the framework types cannot hold more (documented, not silent):

| XSD allows | This library | Why |
| --- | --- | --- |
| Years above 9999, year `0000` is illegal, negative years | Year is exactly four digits, `0001`–`9999` | `DateTime` range. |
| `duration` with years and months (`P1Y2M`) | Rejected, including `P0Y` | A year or month has no fixed length, so it cannot be a `TimeSpan`. |
| Arbitrary-precision `decimal` and `integer` | Range of the .NET type | Out of range is a parse failure, never a silent wrap. |
| `xs:dateTime` / `time` with a date-only or time-only type and a zone | Zone is validated, then dropped | `DateOnly` and `TimeOnly` have no zone. |

`xsi:nil="true"` means the element has no value. `GetString()` returns `null`; every typed getter on a nil element throws.

See [serializer enums](07-api-serializer.md#enums).

---

[Index](README.md) · [Previous: Identity and decisions](01-identity-and-decisions.md) · [Next: Architecture](03-architecture.md)
