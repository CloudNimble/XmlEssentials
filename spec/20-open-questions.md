# Open questions

Part of the [XmlEssentials spec](README.md).

- **Docs hostname.** Not a spec concern. `PackageProjectUrl` is the GitHub repo. Docs pages that need a site URL use `(docs site TBD)`.
- **Package icon.** None. Not a release gate.
- **Lifetime of a decoded `ValueSpan`, and reader copies.** `Utf8XmlReader` is a struct, so `var lookahead = reader;` compiles and is the usual `Utf8JsonReader` look-ahead idiom. A value that needed decoding (an entity, a normalized line end, attribute whitespace) lives in a buffer the copy shares. Advancing either reader to another decoded value overwrites it, so the other reader's current `ValueSpan` changes underneath it. Undecoded values are slices of the input and are unaffected. This was already true for text; attribute values now behave the same way (they used to get an array each). Options: (a) specify it: a decoded `ValueSpan` is valid until this reader **or a copy of it** next advances; (b) allocate a fresh buffer for every decoded token, which is safe and costs an allocation per escaped value; (c) keep the buffer in a small shared object that records which token owns it, and re-decode on access when the owner differs. (c) is the only one that is both safe and cheap. Not decided; no test pins the current behavior.
- **Leading `+` on integers and decimals.** XML Schema allows it (`+5` is a valid `xs:int`) and the lexical routines now accept it. The earlier draft of [XML fidelity](02-xml-fidelity.md) rejected it on purpose. If the stricter rule was deliberate (for example to match what the writer emits), restore it in `XmlLexical` and say so in the departures table.
- **Writer overloads without a namespace.** `WriteStartElement(localName)` inherits the default namespace in scope (as `System.Xml.XmlWriter` does for a null namespace). There is no public way to say "no namespace" under a default namespace; the DOM and node tree do it through internal calls. Decide whether an explicit empty namespace argument should emit `xmlns=""`.
- **Fixture licensing.** Some fixture files are third-party content carried by the repositories they came from (MLB data, RSS feeds, James Clark's xmltest collection, Sun and IBM conformance tests). See `Fixtures/README.md`. Decide whether they may be committed to a public repository or should be fetched in CI.
- **UTF-8 attribute overloads.** `WriteAttributeString(ReadOnlySpan<byte>, ReadOnlySpan<byte>)` takes an unprefixed name only. A prefixed or namespaced attribute needs the `string` overloads, so a zero-allocation reader-to-writer copy cannot be written against the public surface (the DOM uses internal calls). Decide whether to add `WriteAttributeString(utf8Prefix, utf8LocalName, utf8NamespaceUri, utf8Value)`.

---

[Index](README.md) · [Previous: Operability](19-operability.md) · [Next: References](21-references.md)
