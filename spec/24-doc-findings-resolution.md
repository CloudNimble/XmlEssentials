# Doc findings resolution

Part of the [XmlEssentials spec](README.md).

The documentation pass (`681f0ac`) wrote a sample and a page for every public member, and in doing so found places where the library does not do what a reader would expect. This task records every finding, deduplicated, and tracks its resolution. The raw notes were in `review/2026-11-23-documentation-findings.md`, which is not committed; this page is the record.

## Rules for this task

- A finding is closed by a test that states the correct behavior and asserts the exact result, written before the fix ([testing](14-testing.md)).
- Every permutation of a finding is tested: each overload, each placement (root, member, wrapped item, unwrapped item), text and attribute values, XML 1.0 and XML 1.1 where the behavior touches characters.
- The documentation pass described several defects as behavior, in doc comments and guides ("The writer does not use this value"). A fix corrects those statements in the same change.
- No speed work and no benchmark runs belong to this task. The tables in [optimization](23-optimization.md) were **not** regenerated for slice 1.

## Status

| Slice | Content | Status |
| --- | --- | --- |
| 0 | Comment-handling default (F0) | **Done** before this task (`681f0ac`) |
| 1 | F1 to F10: the ten that break or silently change callers | **Done**: 4513 tests pass; coverage gate 100% line, 100% branch |
| 2 | F11 to F40: remaining defects | F24 done and F38 partly done, both with slice 1; the rest open |
| 3 | D1 to D6: decisions (dead API, spec disagreements, round-trip rule) | Open |

## Slice 0: already fixed

| Id | Status | Finding | Resolution |
| --- | --- | --- | --- |
| F0 | **Done** | `XmlCommentHandling.Disallow` was the zero value, so `default(XmlReaderOptions)`, `default(XmlDocumentOptions)` and `default(XmlReaderState)` rejected comments while `new()` skipped them. `XmlDocument.Parse(xml)` threw on any comment. | `Allow = 0`. `default` and `new()` agree. |

## Slice 1: fix first

| Id | Status | Finding | Fix | Tests |
| --- | --- | --- | --- | --- |
| F1 | **Done** | **Interceptors emit the wrong signature.** `TransformInvocation` matched every one- or two-parameter `Serialize<T>` / `Deserialize<T>` call and always emitted `(string, XmlSerializerOptions?)`, so a call with UTF-8 bytes, a stream, a sequence, a reader or a DOM value failed to compile (CS9144). Seen by five agents. | The interceptor takes its parameter type, `ref` kind and return type from the intercepted overload. | `XmlSerializerInterceptorTests` |
| F2 | **Done** | **Interceptors ignore custom converters.** `Serialize(value, optionsWithConverter)` for a type in a context ran the generated converter, wrote the wrong XML and read back default values. | A call that passes options is not intercepted, so the options' converters apply. | `XmlSerializerInterceptorTests` |
| F3 | **Done** | **Interceptors mutate the caller's options.** Each call ran `new Context(options)`, which overwrote `options.TypeInfoResolver`, and threw `InvalidOperationException` on a read-only instance such as `XmlSerializerOptions.Web`. | A call that passes options is not intercepted. Nothing constructs a context around caller options. | `XmlSerializerInterceptorTests` |
| F4 | **Done** | **Generated code ignores `[XmlRoot]` names and namespaces and member namespaces.** `[XmlRoot("purchaseOrder")]` wrote `<PurchaseOrder>`; a namespace on `[XmlRoot]`, `[XmlElement]`, `[XmlAttribute]` or `[XmlArray]` was dropped. With F1 to F3, adding a context changed the XML of existing calls. | One root-name rule for every type info (`XmlTypeInfo.CreateRootName`); generated converters write member, attribute, wrapper and item namespaces. | `XmlSerializerGeneratedNamesTests` |
| F5 | **Done** | **Constructor parameters are matched to members case-sensitively.** `Measurement(string name, double value)` with properties `Name` / `Value` serialized but threw `MissingMethodException` on read; `[XmlConstructor]` with camelCase parameters threw `NotSupportedException` on write. | A parameter fills the member with the same C# or XML name without regard to case, in the reflection binder and in the generator. | `XmlSerializerConstructorCaseTests`, `GeneratedContext_ConstructorParameters_FillMembersWhateverTheirCase` |
| F6 | **Done** | **An abstract class or an interface cannot be the declared polymorphic type.** `[XmlPolymorphic]` with `[XmlDerivedType<Circle>]` on abstract `Shape` threw `NotSupportedException`. | An abstract class or interface that declares derived types is bound for its members and never created. | `XmlSerializerAbstractPolymorphismTests` |
| F7 | **Done** | **The writer emits a name in the wrong namespace.** `WriteStartElement(prefix, localName, ns)` wrote an undeclared prefix when `ns` was already bound to another prefix. An unprefixed element given an empty namespace under a default namespace got no `xmlns=""`. | The prefix given is always written and declared unless already bound to the namespace; an empty namespace writes `xmlns=""` under a default namespace; a shadowed binding is not reused. | `Utf8XmlWriterNamespaceTests` |
| F8 | **Done** | **Segment writers ignore `isFinalSegment`.** Each base64 segment was padded on its own (`AQ==Ag==` for `AQI=`), a UTF-8 character split across segments threw, and a surrogate pair split across segments became U+FFFD. | The writer carries an incomplete character or base64 group to the next segment. | `Utf8XmlWriterSegmentTests` |
| F9 | **Done** | **`ValueSpan` and `GetString()` on a start tag return the previous token's bytes.** | `Read` and the leaf read clear the value. | `ValueMembers_*` in `Utf8XmlReaderTests`, `TryReadLeafContent_LeavesNoValueOnTheEndTag` |
| F10 | **Done** | **`IgnoreCycles` writes nil for every repeated object, not only a cycle, and `Preserve` does not cover collection items** (the same object twice in a list was written twice with one id and read back as two objects). | Reference handling moved into the object converter, so it applies wherever an object is written; `IgnoreCycles` tracks the objects being written, not every object seen. | `XmlSerializerReferenceTests` |

Decisions made in slice 1 are listed under [Slice 1 decisions](#slice-1-decisions).

## Slice 2: fix next

### Serializer: written but not read back

| Id | Status | Finding |
| --- | --- | --- |
| F11 | Open | `Int128`, `UInt128`, `Half` write an empty element and read as zero; `BigInteger` writes its properties, not its value. |
| F12 | Open | `KeyValuePair<,>` writes `Key` / `Value` and reads back default; `Tuple<,>` throws `MissingMethodException` on read; `ValueTuple` writes empty unless `IncludeFields`. |
| F13 | Open | `Queue<T>`, `Stack<T>`, `LinkedList<T>`, `ISet<T>`, `ReadOnlyCollection<T>`, `ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableDictionary<,>`, `IReadOnlyDictionary<,>` serialize and cannot be deserialized. `IReadOnlyDictionary<,>` is written as a list of pairs. |
| F14 | Open | `Memory<byte>` / `ReadOnlyMemory<byte>` throw an internal `ArgumentException`. |
| F15 | Open | A white-space `char` does not round-trip (`<Char> </Char>` reads as "not a single character"). |
| F16 | Open | A null dictionary value does not round-trip (`<a/>` reads as `""`). |
| F17 | Open | A getter-only collection property is silently not filled on read. |
| F18 | Open | `Serialize(XmlElement)` writes the struct's properties; `Serialize(XmlNode)` throws. |
| F19 | Open | An anonymous type as the root throws `XmlException` "Invalid XML name." |
| F20 | Open | `Serialize<object>(value)` throws instead of using the run-time type. |
| F21 | Open | `[property: XmlIgnore]` on a positional record parameter breaks deserialization. |
| F22 | Open | `DictionaryKeyPolicy` is applied on write only. `PropertyNameCaseInsensitive` does not apply to the document element. |

### Serializer: polymorphism, references, nil

| Id | Status | Finding |
| --- | --- | --- |
| F23 | Open | `Deserialize<Book>("<Disc>…")` throws `InvalidCastException`: a derived type inherits the base's derived-type list. |
| F24 | **Done** | A custom `XmlReferenceHandler` never writes `id` / `ref` (the write path compared the handler with `Preserve`). **Fixed in slice 1 with F10**, because the same lines changed. |
| F25 | Open | `SerializeAsyncEnumerable` / `DeserializeAsyncEnumerable` ignore `ReferenceHandler`. |
| F26 | Open | A null `[XmlArray]` collection: `NilHandling.Omit` writes an empty wrapper (reads back as an empty list); `WhenWritingDefault` writes `xsi:nil`. |
| F27 | Open | `DefaultIgnoreCondition = Always` is a silent no-op. |
| F28 | Open | `XmlUnknownDerivedTypeHandling.FallBackToNearestAncestor` applies on write only. |
| F29 | Open | `[XmlCData]` with `]]>` in the value throws instead of splitting the section. |
| F30 | Open | `SerializeAsyncEnumerable` with `DefaultNamespace` writes the wrapper in no namespace and `xmlns` on every item. |

### Generated contexts and converters

| Id | Status | Finding |
| --- | --- | --- |
| F31 | Open | A context answers for unlisted types through reflection, even with `IsReflectionEnabledByDefault = false`. `TypeInfoResolverChain` therefore never reaches a second context. |
| F32 | Open | Generated converters ignore run-time options: member naming policy, `NilHandling.Omit`, `DefaultIgnoreCondition`, `options.Converters`, type-level and member-level `[XmlConverter]`. |
| F33 | Open | A context built from caller options loses the XSD target namespace. The root namespace now comes from `[XmlRoot]` on the generated class (F4), which should cover the document element; not verified. |
| F34 | Open | A custom-converted type cannot be an attribute or text ("Complex types cannot be attributes."). |
| F35 | Open | The source generator ignores `XmlElementAttribute.IsNullable`; the reflection binder ignores `XmlElementAttribute<T>.Order` / `IsNullable`. |
| F36 | Open | Unverified: `XsdParser` shares one class-name set across `.xsd` files. Native AOT publish was not run by the documentation agents. The publish has since run and passed in slice 1 (`XmlAotPublishTests`); the class-name question is still unverified. |

### Reader, writer, DOM

| Id | Status | Finding |
| --- | --- | --- |
| F37 | Open | Reader: `CurrentDepth` differs for `<b/>` (1) and `<c></c>` (2) at the same level, and the root end tag reports 0. `Skip()` / `TrySkip()` on a token that is not a start tag acts as `Read()`. |
| F38 | Partly done | Writer: a non-ASCII `IndentCharacter` is truncated to a byte; `Dispose` with open elements throws before flushing; some string fast paths skip the disposed check (`WriteStringValue(string)` and `WriteStringValue(ReadOnlySpan<char>)` were fixed in slice 1 with F8 and are pinned by `StringValue_AfterDispose_IsRejected`; the attribute fast paths remain); `WriteRawValue` never validates; an indented comment or processing instruction is not put on its own line. |
| F39 | Open | DOM: `XmlDeclarationNode.WriteTo` writes two declarations; a declaration or attribute node added to `Children` is written in place with no error; `XmlDocument.WriteTo` ignores `OmitXmlDeclaration`; `SerializeToElement` leaks pooled buffers; the list indexer setter orphans the old item when the new one is rejected; `SetAttribute` and the name lookups with no namespace match any namespace; `XmlNode.ToString()` writes XML 1.0 for a tree parsed from 1.1; `XmlProcessingInstructionNode.Target` setter does not validate; `XmlNode.Parse` is typed nullable and never returns null; `XmlDocument.ParseValue` can return a rootless document. |
| F40 | Open | Input: a string whose declaration says `encoding="utf-16"` is rejected (what `System.Xml` writes to a `StringWriter`); UTF-16 bytes with a BOM get the wrong message; negative limits are not validated; `1e400` reads as infinity unless `XmlNumberHandling.Strict`; a lone surrogate is written as U+FFFD with no error. |

## Slice 3: decisions

| Id | Status | Decision needed |
| --- | --- | --- |
| D1 | Open | **Public API that nothing reads: implement or remove.** `XmlSerializerOptions.AllowOutOfOrderMetadataProperties`, `InferClosedTypePolymorphism`, `RespectNullableAnnotations`, `PreferredObjectCreationHandling`, `UnknownTypeHandling`; ten `XmlSourceGenerationOptionsAttribute` properties; `XmlSerializableAttribute.GenerationMode` (read, then discarded); `XmlArrayAttribute.IsNullable`, `XmlRootAttribute.IsNullable`, `XmlArrayItemAttribute.Namespace`, `XmlDerivedTypeAttribute.Namespace`, `XmlPolymorphicAttribute.InferClosedTypePolymorphism`; `skipInputValidation`; `XmlValueKind.Document`; `HasValueSequence` / `ValueSequence`; `XmlException.Path`. |
| D2 | Open | **Metadata mode.** It calls the reflection resolver at run time, so it is not AOT-safe, and the three generation modes emit the same code. |
| D3 | Open | **Round-trip rule.** `AGENTS.md` says what is parsed is written back byte for byte. A probe lists seven ways it is not: white space outside the root, `<B />`, attribute quote style, CRLF, character references, `<b></b>` from a mutable tree, the declaration. Change the rule or the DOM. |
| D4 | Open | **Spec disagreements.** Attributes and types the spec names and the code does not have (`XmlNamespaceDeclaration`, `XmlNamespace`, `XmlNamespaceDeclarations`, `XmlType`, `XmlNamingPolicyAttribute`, `XmlPolymorphismOptions`, `XmlDerivedTypeInfo`); `float` / `double` format; `DateTimeOffset` zero offset; `MaxTextByteCount` scope; `[XmlArrayItem]` without `[XmlArray]`; enum names and naming policy; `[XmlMixed]` and `[XmlExtensionData]` member types; the XSD mapping of `xs:choice`, `substitutionGroup`, `xs:extension`, `minOccurs`; sequence root names (`ArrayOfX`, `ListOfX`, `XArray`). |
| D5 | Open | **Marked "possibly intended".** The discriminator written when a derived type is serialized as itself; non-public setters used on read; a constructor that binds members winning over a public parameterless one (System.Text.Json prefers the parameterless one); `Reset()` committing pending bytes; the declaration version check accepting any `1.x`. |
| D6 | Open | **Wish list.** Missing `WriteElementString` / `WriteElementNumber` overloads, `ParseAsync`, typed getters on `XmlElement`, a namespace prefix choice in the serializer, generated converters for derived types and `ReferenceHandler`. |

## Slice 1 decisions

Each of these changes what a caller sees. They are written into the spec pages named.

1. **Interceptors apply only to a call that passes no options** ([K42](01-identity-and-decisions.md#key-decisions), [source generation](10-source-generation.md)). The old interceptor built a context around the caller's options, which is what mutated them, threw on read-only instances and bypassed their converters. Generated converters do not honor run-time options (F32), so no design that sends caller options into generated code can be correct yet. A call that passes options, even `null`, now runs exactly as written. This removes a documented behavior: `Serialize(value, options)` no longer makes a context the options' resolver. To use generated code with your own options, pass `new MyContext(options).T`.
2. **One rule names the document element** ([source generation](10-source-generation.md)). `[XmlRoot]` is read for every type info, so it now also applies to a type that a caller's own `XmlConverter<T>` writes, and to XSD-generated classes used through a context built from caller options (part of F33; not separately tested).
3. **Writer namespaces follow `System.Xml.XmlWriter`** ([XML fidelity](02-xml-fidelity.md)). A probe of `XmlWriter` settled the open question: a `null` namespace inherits, an empty string means no namespace and writes `xmlns=""` under a default namespace. The span overloads cannot say `null`, so an empty span means no namespace; the overloads with no namespace parameter inherit. A prefix that an inner element rebound is no longer reused for an unprefixed element or an attribute, and `WriteNilValue` declares `xsi` unless the `xsi` prefix itself is bound to the schema-instance namespace. The open question is removed from [open questions](20-open-questions.md).
4. **A segmented value is one kind, and nothing may interrupt it** ([writer](05-api-writer.md#segmented-values)). Mixing UTF-8 and character segments in one value was accepted before and is now rejected, as `Utf8JsonWriter` rejects it. `Write_SegmentsAndRaw` wrote such a value and now writes UTF-8 segments only.
5. **`]]>` is escaped across calls** ([writer](05-api-writer.md#segmented-values)). `WriteStringValue("]]")` followed by `WriteStringValue(">")` wrote `]]>` into character data, which is not well formed. Found while testing F8; it did not need segments.
6. **Constructor parameters match members without regard to case, always** ([object mapping](09-object-mapping.md)). `PropertyNameCaseInsensitive` now only governs names in the document. A parameter may also name a member by its XML name when the constructor is chosen, as the generator already allowed. Consequence: a type with a public parameterless constructor and a constructor whose camelCase parameters all name members is now created through the second one, because the rule "most parameters wins" did not change. `CamelCaseParameter_NextToAParameterlessConstructor_IsTheConstructorUsed` pins it; D5 asks whether the rule itself should change. A type with two public constructors of the same size whose parameters all name members, one of which matched only now, throws `NotSupportedException` asking for `[XmlConstructor]`.
7. **An abstract class or an interface must declare its derived types** ([object mapping](09-object-mapping.md#polymorphism)). With none it stays unsupported. With some, an element that identifies none of them is an `XmlException`, where a concrete base would be created.
8. **Reference handling is per object, wherever it is written** ([serializer](07-api-serializer.md)). Value types, collections, and values written by a caller's own converter are not tracked (a member written by a caller's converter was tracked before). Before, a collection that two members shared was written as a `ref` to an `id` nothing declared, which could not be read back. A caller-defined handler now writes `id` / `ref` from its own identifiers (F24).
9. **The reader's value members are empty on a start tag and an end tag.**

Permutations that were already correct, and are now pinned: a prefix in scope for another namespace is redeclared; `WriteNilValue` with `xsi` bound to another namespace; an `IgnoreCycles` or `Preserve` cycle through a member; the constructor rule for members that differ only by case; an abstract type with no derived types is rejected.

Sample tests whose expected output changed, with their samples and guides: `Sample11_ImmutableTypes_ParameterNameCase`, `Sample17_PreserveReferences_UseCustomResolver`, `Sample31_ReflectionVsSourceGeneration_Interceptors`.

One statement was removed because it could not run, and it is recorded here because the testing rules ask for a question before a gap is called unreachable. In `WriteStringValueSegment(ReadOnlySpan<byte>, bool)`, written in this slice, the branch for bytes that are not a character called `WriteStringValue(joined)` and then advanced `value`. That call always throws "Invalid UTF-8 in XML value.", so the branch now throws that exception itself. `Utf8Segments_AtEverySplit_MatchOneCall` compares the outcome with the single call for invalid and truncated input.

Hot paths changed and were not measured: element text writes (the `]]>` count), `XmlObjectConverter<T>.Write` (reference handling), and type-info creation (one `[XmlRoot]` lookup per type). The ratios in [PR plan](22-pr-plan.md) and [optimization](23-optimization.md) are from before this slice.

The Native AOT smoke (`XmlAotPublishTests`, on the net11.0 test host) publishes and runs `CloudNimble.XmlEssentials.Tests.Aot` and passed with these changes. No benchmark was run.

---

[Index](README.md) · [Previous: Optimization](23-optimization.md)
