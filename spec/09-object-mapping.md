# Object mapping rules

Part of the [XmlEssentials spec](README.md).

## Null, empty, and nillable

JSON has one null. XML has four shapes:

| XML | Meaning on deserialize (string property) | Written when |
| --- | --- | --- |
| omitted member | default / null per `DefaultIgnoreCondition` and `required` | `WhenWritingNull` / `WhenWritingDefault` |
| `<Name xsi:nil="true" />` or `<Name xsi:nil="true"></Name>` | `null` | null value, property nillable, not ignored |
| `<Name/>` or `<Name></Name>` | `""` (empty string), not null | empty string |
| `<Name>Alice</Name>` | `"Alice"` | non-empty |

Nillable if any of:

- `T?` (Nullable reference or `Nullable<T>`),
- `[XmlElement(IsNullable = true)]` / `[XmlRoot(IsNullable = true)]`,
- `RespectNullableAnnotations` and the property is annotated nullable.

`required` members and `[XmlRequired]` cannot be nil; nil is `XmlException`. **Missing** required member / required constructor parameter on deserialize is also `XmlException` ([K37](01-identity-and-decisions.md#key-decisions)). `RespectRequiredConstructorParameters` default **true**.

`xsi:nil` plus other attributes on the same element is **legal**. Deserialize: map attributes, set the element value to null. `WriteNilValue` after attributes: legal; after content: `XmlException`.

`XmlSerializerOptions.NilHandling`:

- `WriteXsiNil` (default when the value is null and not ignored),
- `WriteEmptyElement` (write `<e/>` for null — **not** recommended; exists for broken interop),
- `Omit` (treat null as omit even if `DefaultIgnoreCondition` is `Never`).

Default stack: `DefaultIgnoreCondition = Never`, `NilHandling = WriteXsiNil`.

**Null root ([K31](01-identity-and-decisions.md#key-decisions)):** `Serialize<T>(null)` for a nillable reference type writes the root element with `xsi:nil="true"`. If `NilHandling` is `Omit`, throw `InvalidOperationException`.

Serializer `WhitespaceHandling` is **reader-only** ([K38](01-identity-and-decisions.md#key-decisions)).

## Root element

1. `[XmlRoot("Name", Namespace = "...")]` if present.
2. Else `ElementNamingPolicy.ConvertName(type.Name)` (or `PropertyNamingPolicy` if element policy is null). CLR name: `Int32` not `int` ([K25](01-identity-and-decisions.md#key-decisions)).
3. Generic type names: strip `` `1 `` and join `Of{Arg}` (`ListOfPerson`).
4. `XmlSerializerOptions.DefaultNamespace` applies when `[XmlRoot].Namespace` is null.

## Member selection

Serialize a member if:

1. It is a public instance property with a getter, or a public instance field when `IncludeFields`, or a non-public member with `[XmlInclude]`.
2. It is not `[XmlIgnore]` / `Condition` matches.
3. If read-only (no setter, not `init`, not constructor-bound): skipped when `IgnoreReadOnlyProperties` / `IgnoreReadOnlyFields`.
4. `init` and `required` members are included (match STJ).
5. Records: compiler-generated parameters bind like STJ.
6. Record structs and structs are supported. Parameterless constructor is not required if `[XmlConstructor]` or a unique parameterized constructor can bind all required members.

Deserialize needs: setter, or `init`, or constructor parameter (name match, case-insensitive if `PropertyNameCaseInsensitive`), or `[XmlConstructor]`.

Multiple parameterized constructors: `[XmlConstructor]` wins; else the public constructor whose parameters are a subset of serializable members, preferring most parameters. Ambiguity is `NotSupportedException` at type-info build.

## Name resolution

```
name = XmlPropertyName
    ?? XmlAttribute.AttributeName
    ?? XmlElement.ElementName
    ?? XmlArray.ElementName
    ?? Policy.ConvertName(member.Name)
```

Namespace:

```
ns = XmlAttribute.Namespace | XmlElement.Namespace | XmlArray.Namespace
    ?? declaringType XmlType.Namespace
    ?? declaringType XmlNamespace
    ?? XmlRoot.Namespace (root only)
    ?? options.DefaultNamespace
    ?? ""
```

## Element vs attribute vs text

| Annotation | Mapping |
| --- | --- |
| (none) | child **element** |
| `[XmlAttribute]` | XML **attribute** (simple types only). Complex types throw at type-info build. |
| `[XmlText]` | simple content of the current element |
| `[XmlCData]` | like text, written as CDATA |
| `[XmlArray]` | wrapped collection |
| `[XmlElement]` | element with extra metadata (still default kind) |

Illegal combinations (type-info build error): `[XmlAttribute]+[XmlElement]`, `[XmlAttribute]+[XmlText]`, `[XmlText]+[XmlArray]`, two `[XmlText]` on one type, `[XmlAttribute]` on a complex type.

A type may have many `[XmlAttribute]` members, many element members, and **at most one** `[XmlText]`/`[XmlCData]` **or** `[XmlMixed]`. Mixed-content mapping: [below](#mixed-content).

**Multiple `[XmlElement]` / `[XmlArrayItem]` ([K35](01-identity-and-decisions.md#key-decisions)):** `AllowMultiple = true` means per-member polymorphic **choice**. Each extra must set `Type`. `ElementName` is the discriminator for that type (same idea as `[XmlDerivedType]` scoped to the member). If `Type` is missing on a duplicate, type-info build error.

## Order

1. XML attributes first. Attribute order: `[XmlPropertyOrder]` if `>= 0`, else declaration order.
2. Then `[XmlText]` / `[XmlCData]` if simple content.
3. Then child elements.

Child element order ([K9](01-identity-and-decisions.md#key-decisions), [K36](01-identity-and-decisions.md#key-decisions)):

1. Members with explicit order (`[XmlPropertyOrder]`, else `[XmlElement.Order]` / `[XmlArray.Order]`) where order **`>= 0`**, sorted by that integer (stable).
2. Members with **unset** order (`-1`, the default — **not** `0`) in **flattened declaration order**: most-base to most-derived; within a type, source declaration order (source-gen: `INamedTypeSymbol` declaration order; reflection: metadata token).

`[XmlPropertyOrder]` **may interleave** base and derived. Default walk without Order is base-then-derived (XSD-shaped). The break from BCL is that Order is **global** on the flattened type, not per-class.

Example (required test — derived **between** two base properties):

```csharp
public class Base
{
    [XmlPropertyOrder(0)] public string A { get; set; }
    [XmlPropertyOrder(2)] public string C { get; set; }
}
public class Derived : Base
{
    [XmlPropertyOrder(1)] public string B { get; set; }
}
```

```xml
<Derived>
  <A>a</A>
  <B>b</B>
  <C>c</C>
</Derived>
```

BCL `XmlSerializer` cannot emit this (derived appends). This library can.

## Collections

Supported collection types, matching STJ:

- `T[]`, `List<T>`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>` (serialize only unless concrete `List<T>` or `T[]` on deserialize)
- `IReadOnlyList<T>`, `IReadOnlyCollection<T>`
- `Collection<T>`, `ObservableCollection<T>`
- `ImmutableArray<T>`, `ImmutableList<T>` (if referenced by the consumer)
- `IAsyncEnumerable<T>` via `SerializeAsyncEnumerable` / `DeserializeAsyncEnumerable` only (not as a property type unless the member converter handles it)
- `Dictionary<TKey,TValue>`, `IDictionary<TKey,TValue>`, `IReadOnlyDictionary<TKey,TValue>` as **maps** ([K16](01-identity-and-decisions.md#key-decisions))

**Dictionary mapping (STJ JSON object analogue):**

1. Apply `DictionaryKeyPolicy` (else `PropertyNamingPolicy`) to the key.
2. If `TKey` is `string` (or converts to string) **and** the converted key is a valid XML `Name`: write a child element with that local name and the value as content.
3. Otherwise write repeating entries:

```xml
<Scores>
  <Key>not a name</Key>
  <Value>10</Value>
</Scores>
```

`[XmlArrayItem]` / `[XmlElement]` on a dictionary member can rename the entry wrapper. Null dictionary: omit or `xsi:nil` per nil/ignore rules.

**Not maps:** `[XmlExtensionData]` (`XmlExtensionData` or `Dictionary<string, XmlElementNode>` **only** with that attribute) and `[XmlNamespaceDeclarations]`. Generator must not treat those as dictionary maps.

**Default (unwrapped, STJ-like):** `List<string> Items` → repeated `<Items>`.

**Wrapped:** `[XmlArray("Items")]` `[XmlArrayItem("Item")]` → wrapper + item names.

`[XmlArrayItem]` without `[XmlArray]` **renames** the repeated element.

Empty vs null:

| Value | Unwrapped | Wrapped |
| --- | --- | --- |
| `null`, ignore WhenWritingNull | omit | omit |
| `null`, nillable, Never | omit unless `[XmlArray]` | `<Items xsi:nil="true"/>` |
| empty | omit | `<Items/>` |

Unwrapped empty and unwrapped null both omit. Prefer wrapped collections or non-null empty lists.

## Mixed content

`[XmlMixed]` on a member of type `List<XmlNode>`, `List<object>`, or `IList<XmlMixedContentItem>` maps interleaved text and elements:

```csharp
public sealed class Paragraph
{
    [XmlMixed]
    public List<XmlMixedContentItem> Content { get; set; }
}

public abstract class XmlMixedContentItem { }
public sealed class XmlMixedText : XmlMixedContentItem { public string Text { get; set; } }
public sealed class XmlMixedElement : XmlMixedContentItem { public Nodes.XmlElementNode Element { get; set; } }
```

POCOs in the list use `[XmlDerivedType]` / `[XmlElement]` for child element names. Text nodes become `XmlMixedText`. A type with `[XmlMixed]` **and** ordinary element properties is `NotSupportedException` at type-info build. Simple content (`[XmlAttribute]` + `[XmlText]`) stays the leaf-element case.

## Polymorphism

```csharp
[XmlPolymorphic] // DiscriminatorPlacement = ElementName
[XmlDerivedType(typeof(Book), "Book")]
[XmlDerivedType(typeof(Dvd), "Dvd")]
public class Product
{
    public string Title { get; set; }
}
```

**ElementName (default):** the **element name itself** is the discriminator. An unwrapped `List<Product> Products` emits derived names, not `Products`:

```xml
<Catalog>
  <Book>
    <Title>Clean Code</Title>
    <Author>Robert C. Martin</Author>
  </Book>
  <Dvd>
    <Title>The Matrix</Title>
    <Runtime>136</Runtime>
  </Dvd>
</Catalog>
```

Wrapped with `[XmlArray("Items")]`:

```xml
<Catalog>
  <Items>
    <Book>...</Book>
    <Dvd>...</Dvd>
  </Items>
</Catalog>
```

**Attribute placement:**

```csharp
[XmlPolymorphic(TypeDiscriminatorPropertyName = "kind", DiscriminatorPlacement = XmlDiscriminatorPlacement.Attribute)]
[XmlDerivedType(typeof(Book), "book")]
```

```xml
<Products kind="book">...</Products>
```

**XsiType placement:** writes `xsi:type="Book"` (our writer, not `System.Xml`).

Unknown derived types: `UnknownDerivedTypeHandling` (default `FailSerialization`).

Per-member `[XmlElement(typeof(Book), "Book")]` duplicates this for one property ([K35](01-identity-and-decisions.md#key-decisions)).

## Enums

- Default: write member **name** after `PropertyNamingPolicy` (or `EnumHandling.Name`).
- `[XmlPropertyName("foo")]` on the enum field overrides the name.
- `[Flags]`: space-separated names (BCL convention).
- `EnumHandling.Integer`: XSD integer.
- Unknown names on read: `XmlException`.

## Extension data

```csharp
[XmlExtensionData]
public XmlExtensionData? Extra { get; set; }

public sealed class XmlExtensionData
{
    public Dictionary<XmlName, string> Attributes { get; }
    public List<Nodes.XmlElementNode> Elements { get; }
}
```

Also accept `Dictionary<string, XmlElementNode>` (local-name keys, default ns) **only** when `[XmlExtensionData]` is present. Unknown members go here when `UnmappedMemberHandling.Skip`. When `Disallow` and no extension bag, unknown elements/attributes throw. `xmlns*` attributes are never extension data.

## Namespaces on write

1. `[XmlNamespaceDeclaration("addr", "http://example.com/address")]` on the type → `xmlns:addr` on that element.
2. `options.DefaultNamespace` / `[XmlRoot].Namespace` / `[XmlNamespace]` → default `xmlns`.
3. Writer auto-declares remaining URIs.
4. `[XmlNamespaceDeclarations]` on `Dictionary<string,string>` or `List<(string prefix, string uri)>` round-trips xmlns (BCL parity; Dictionary here is the K16 carve-out).

Name comparison for mapping is always `{local, ns}`, never prefix.

## Built-in primitives (serializer)

Same as reader getters, plus `string`, `char`, `Uri`, `Version`, `byte[]` (base64), `enum`.

`object` properties: only with a converter, or `UnknownTypeHandling` to `XmlElement`/`XmlNode`.

## What we do not honor

- Any `System.Xml.Serialization.*` attribute.
- `IXmlSerializable`, `ISerializable`, DataContract attributes.
- `ShouldSerializeX()` / `XSpecified`. Use ignore conditions / converters.
- `XmlAttributeOverrides`.

## Migration from BCL XmlSerializer (guide contract)

| BCL | XmlEssentials |
| --- | --- |
| `using System.Xml.Serialization` | `using CloudNimble.Text.Xml.Serialization` |
| `[XmlRoot]` | `[XmlRoot]` (our ns) |
| `[XmlElement("N")]` | `[XmlPropertyName("N")]` or `[XmlElement("N")]` |
| `[XmlAttribute]` | `[XmlAttribute]` (our ns) |
| `[XmlIgnore]` | `[XmlIgnore]` |
| `[XmlArray]` / `[XmlArrayItem]` | same names, our ns; **default collection shape differs** (unwrapped) |
| `[XmlInclude(typeof(T))]` | `[XmlDerivedType(typeof(T), "T")]` |
| `[XmlEnum("n")]` | `[XmlPropertyName("n")]` on the field |
| `[XmlText]` | `[XmlText]` |
| `[XmlType]` | `[XmlType]` |
| public types only | internal OK with source-gen (consuming compilation) |
| base-then-derived Order | default same; `[XmlPropertyOrder]` may interleave |
| sgen sidecar | `[XmlSerializable]` context |

---

[Index](README.md) · [Previous: Converters and attributes](08-api-converters-and-attributes.md) · [Next: Source generation](10-source-generation.md)
