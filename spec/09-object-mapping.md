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

A reference type and `Nullable<T>` are nillable. `[XmlElement(IsNullable = true)]` marks a member nillable as well.

**`RespectNullableAnnotations`** (default `false`) refuses a null where the annotation says there is none, with the rules of `JsonSerializerOptions.RespectNullableAnnotations`, and throws `XmlException`:

- Reading: an element marked `xsi:nil="true"` for a member that does not accept null ("The property or field 'Name' on type 'T' doesn't allow setting null values. Consider updating its nullability annotation."), or for the constructor parameter it fills ("The constructor parameter 'name' on type 'T' doesn't allow null values. …").
- Writing: a null held by a member annotated as never returning null ("… doesn't allow getting null values. …"), whether it is an element, an attribute or text. A null the ignore condition leaves out is not refused.
- `[AllowNull]` and `[DisallowNull]` change what a member accepts; `[MaybeNull]` and `[NotNull]` what it returns.
- Not checked: a member the document leaves out, collection items, dictionary values, the root, a member compiled without annotations, and a member whose type is a type parameter with no `class`, `struct` or `notnull` constraint.
- The reflection resolver reads the annotations with `NullabilityInfoContext` when the type is bound. The generator reads them from the symbols and emits checks that test the option when the converter runs; both give the same answer for the same member.

`required` members and `[XmlRequired]` cannot be nil; nil is `XmlException`. **Missing** required member / required constructor parameter on deserialize is also `XmlException` ([K37](01-identity-and-decisions.md#key-decisions)). `RespectRequiredConstructorParameters` default **true**.

`xsi:nil` plus other attributes on the same element is **legal**. Deserialize: map attributes, set the element value to null. `WriteNilValue` after attributes: legal; after content: `XmlException`.

**Null is written as `xsi:nil`.** `xsi:nil` is how XML spells `null`, so there is no separate option for it. Whether a null member is written at all is `DefaultIgnoreCondition` and `[XmlIgnore(Condition = …)]`, with the meaning `JsonIgnoreCondition` has in System.Text.Json:

- `Never` (default): a null element member is written, marked `xsi:nil="true"`.
- `WhenWritingNull`: a null member is left out.
- `WhenWritingNullReference`: a null reference is left out; a nullable value type with no value is written as under `Never`, with `xsi:nil`. It is resolved once per member, when the type is bound: `WhenWritingNull` for a reference type, `Never` for a `Nullable<T>`.
- `WhenWritingDefault`: a null member, and a value type holding its default value, is left out.
- `WhenWriting`: the member is never written, and is still read.
- `WhenReading`: the member is never read, and is still written. Its element or attribute is a known one (it is not an unmapped member) and is passed over without being read. A constructor parameter of the same name is still filled. On a required member it is `InvalidOperationException` when the type is bound: "The member 'X' on type 'T' is required, and its ignore condition is XmlIgnoreCondition.WhenReading, so it would never be read."

`DefaultIgnoreCondition` takes every value but `Always`, as in System.Text.Json. As the default for every member, `WhenWriting` and `WhenReading` have no effect there (a probe of .NET 11: both are accepted and neither is applied), and they have none here: a member with no `[XmlIgnore]` is written and read as under `Never`.

The condition applies to members only. A null item of a collection, a null value of a dictionary and a null root are always written, marked `xsi:nil="true"`, as System.Text.Json always writes `null` for them. A null attribute or text member is left out, because only an element can be nil.

**Null root ([K31](01-identity-and-decisions.md#key-decisions)):** `Serialize<T>(null)` for a nillable reference type writes the root element with `xsi:nil="true"`. No ignore condition changes that.

Serializer `WhitespaceHandling` is **reader-only** ([K38](01-identity-and-decisions.md#key-decisions)).

## Root element

1. `[XmlRoot("Name", Namespace = "...")]` if present.
2. Else the policy applied to the **type name** (`ElementNamingPolicy`, or `PropertyNamingPolicy` if that is null). The type name (`XmlTypeNames`, and `XmlTypeNameSymbols` in the generator) is the one `System.Xml.Serialization.XmlSerializer` writes ([K25](01-identity-and-decisions.md#key-decisions), [decision 70](24-doc-findings-resolution.md)):
   - a built-in type has its XML Schema name: `boolean`, `unsignedByte` (`byte`), `byte` (`sbyte`), `short`, `unsignedShort`, `int`, `unsignedInt`, `long`, `unsignedLong`, `float`, `double`, `decimal`, `char`, `string`, `dateTime`, `base64Binary` (`byte[]`), `anyType` (`object`); and, with no schema name, `dateTimeOffset`, `dateOnly`, `timeOnly`, `guid`. `Nullable<T>` has the name of `T`;
   - a collection (anything the serializer writes as a sequence, whatever its type) is `ArrayOf` + the name of its items with a capital first letter and without the characters an identifier cannot hold: `ArrayOfInt`, `ArrayOfPerson`, `ArrayOfArrayOfInt`;
   - anything else has its CLR name, and a generic type is the name + `Of` + the CLR-style names of its arguments: `BoxOfInt32`, `DictionaryOfStringInt32`, `BoxOfListOfInt32`, `BoxOfArrayOfByte`, `BoxOfNullableOfInt32`.
3. `[XmlRoot("x")]` names the type wherever the type names an element, not only as a document: the items of a root collection and of a wrapped collection of the type are `x`, and the collection is `ArrayOfX`. (The BCL uses `[XmlType]` for this, which this library does not have.) The element that holds an asynchronous sequence has the same name as a `List<T>` of the values, so each reads what the other wrote.
4. `XmlSerializerOptions.DefaultNamespace` applies when `[XmlRoot].Namespace` is null.

## Member selection

Serialize a member if:

1. It is a public instance property with a getter, or a public instance field when `IncludeFields`, or a non-public member with `[XmlInclude]`.
2. It is not `[XmlIgnore]` / `Condition` matches.
3. If read-only (no setter the serializer uses, and not constructor-bound): skipped when `IgnoreReadOnlyProperties` / `IgnoreReadOnlyFields`, unless it is a collection or a dictionary, which needs no setter (as System.Text.Json; [decision 71](24-doc-findings-resolution.md)).
4. `init` and `required` members are included (match STJ). A `set` or `init` accessor is used when it is public, or when the member is marked `[XmlInclude]`; one that is not public is not used otherwise, so the property is read-only to the serializer, as in System.Text.Json with `[JsonInclude]` ([decision 79](24-doc-findings-resolution.md)).
5. Records: compiler-generated parameters bind like STJ.
6. Record structs and structs are supported. A struct is created as its default and its members are set, unless a constructor is marked `[XmlConstructor]`. A class needs no parameterless constructor when it has a marked constructor or one public constructor with parameters.

Deserialize needs: setter, or `init`, or constructor parameter (name match against the member's C# name or XML name, always without regard to case as in STJ; an exact-case match wins when two members differ only by case), or `[XmlConstructor]`. A member with none of these is read only when the value it holds is populated (see "Object creation handling"); otherwise its element is passed over without being read, so what it holds does not have to be a value of the member's type. A constructor parameter whose property or field is marked `[XmlIgnore]` takes the default of its type.

`PropertyNameCaseInsensitive` applies to the document element as well as to members, and to an element named for a derived type or for an alternative of an element choice: one rule for every element name (`DerivedElementNames_AreMatchedAsPropertyNameCaseInsensitiveSays`). Until 2026-11-25 a derived element name was matched without regard to case whatever the option said. Ignoring case is an ASCII rule; a name that holds other characters matches only itself.

The constructor is chosen as System.Text.Json chooses it ([decision 81](24-doc-findings-resolution.md)): the one marked `[XmlConstructor]`; else the public parameterless constructor, and a struct is created as its default; else the one public constructor with parameters. More than one public constructor with parameters, none marked and no public parameterless one, is `NotSupportedException` at type-info build. A type with no public constructor is created through its parameterless constructor. `KeyValuePair<,>` is created through its constructor.

### Members of the same name

A type and its base types may declare a member of the same name. The rules are System.Text.Json's, taken from a probe of .NET and pinned by `MembersOfTheSameName_FollowTheRulesOfSystemTextJson`, which runs each case through a generated context and through the reflection resolver.

- **A member is governed by the attributes written on it.** An override does not take the attributes of the property it overrides: not `[XmlIgnore]`, not a name, not an order.
- A member that overrides a base property, or hides a base member with `new` (of the same type or another), is serialized in its place.
- An override marked `[XmlIgnore]` hides the property it overrides, so neither is serialized. This holds down the whole chain of overrides, and for a property that implements an interface member, whose accessors are virtual too.
- A `new` member marked `[XmlIgnore]` does not hide the member it hides: the base member is serialized.
- Attributes are not inherited, so a base property named with `[XmlElement("n")]` and its override with no attribute have different XML names, and both are written.
- Two members that are not related in one of these ways and map to one XML name are `InvalidOperationException`: "The type maps more than one member to the element name 'X'." For elements, a name is a local name and a namespace: two element members may share a local name when their namespaces differ (`link` and `atom:link`). An element then goes to the member that names its namespace, and a member with no namespace takes an element of that local name in any other namespace ([decision 63](24-doc-findings-resolution.md)). For attributes a name is also a local name and a namespace: two attribute members may share a local name when their namespaces differ (`href` and `xlink:href`), and the same local name in the same namespace is "The type maps more than one member to the attribute name 'X'." An unprefixed attribute is in no namespace (Namespaces in XML, section 6.2), so an attribute member with no namespace takes the unprefixed attribute only, and there is no counterpart of the element rule for other namespaces ([decision 84](24-doc-findings-resolution.md)). The exception is a base member whose C# name an ignored member of a more derived type has: it gives way without an error.
- Among members that survive, the order is unchanged: base types first.

The rule is applied once, in `XmlObjectBinder.CreateModels`, to the member facts of either resolver. Generated metadata says whether a property is virtual (`XmlPropertyInfoValues<TDeclaring, TValue>.IsVirtual`, the name System.Text.Json uses).

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
    ?? XmlRoot.Namespace (root only)
    ?? options.DefaultNamespace
    ?? ""
```

## Element vs attribute vs text

Unannotated members follow `XmlSerializerOptions.DefaultPropertyMapping` (default `Element`). That is the blanket switch: set `Attribute` to write scalars as attributes without `[XmlAttribute]` on every property. Nested objects stay **elements**; the same options apply when those types bind, so their unannotated simple members are attributes too (`<Employee Name="Ada"><Home Street="1 Main" City="Paris"/></Employee>`). Collections stay elements. Per-member annotations still win.

| Annotation / default | Mapping |
| --- | --- |
| (none), `DefaultPropertyMapping = Element` | child **element** |
| (none), `DefaultPropertyMapping = Attribute` | XML **attribute** when the member is a simple type; otherwise child element |
| `[XmlAttribute]` | XML **attribute** (simple types only). Complex types throw at type-info build. |
| `[XmlText]` | simple content of the current element |
| `[XmlCData]` | like text, written as CDATA |
| `[XmlArray]` | wrapped collection |
| `[XmlElement]` | element with extra metadata (forces element even when the default is `Attribute`) |

Illegal combinations (type-info build error): `[XmlAttribute]+[XmlElement]`, `[XmlAttribute]+[XmlText]`, `[XmlText]+[XmlArray]`, two `[XmlText]` on one type, `[XmlAttribute]` on a complex type.

A type may have many `[XmlAttribute]` members, many element members, and **at most one** `[XmlText]`/`[XmlCData]` **or** `[XmlMixed]`. Mixed-content mapping: [below](#mixed-content).

**Multiple `[XmlElement]` / `[XmlArrayItem]` ([K35](01-identity-and-decisions.md#key-decisions), [decision 74](24-doc-findings-resolution.md)):** more than one `[XmlElement<T>]` on a member, or more than one `[XmlArrayItem<T>]` on a collection in a wrapper, is a per-member **choice**: each element name stands for a type, exactly as `[XmlDerivedType]` with `ElementName` placement does for a type, and it stands in place of what the member's type declares. A value is written under the element of its type and an element is read as the type of its name; a collection writes each item under its own element and keeps its order. A value of the member's own type is written under the member's name; any other type with no element is `NotSupportedException` on write, except a type that derives from an alternative whose own type declares derived types with `xsi:type` or an attribute: it is written under the element of that alternative with its discriminator, and read back through it (`XmlDerivedBinding.Nested`). An alternative's derived types that are identified by element name do not apply inside a choice. **Named values** ([decision 77](24-doc-findings-resolution.md)): when the member's type, or its item type, is `XmlNamedValue` (`Name`, `Namespace`, `Value`), each value names its element, so the elements may hold any types and two of them one type. The value is written under the element it names and must be of that element's type (or of a type its own derived types declare); a null `Value` is written with `xsi:nil`. A name the member does not have, or a value of another type, is `NotSupportedException` on write. An element is read into an `XmlNamedValue` with its name and the namespace it was declared with. Two elements of one name in one namespace are still refused. Bound when the type is first used, with `NotSupportedException` for: an alternative that is not assignable to the member's type or item type; two elements for one type; one element name (in one namespace) for two types; an attribute among several that names no type. One `[XmlElement<T>]` alone is not a choice: it names the type the member is read as. A type with a choice has no write fast path.

## Order

1. XML attributes first. Attribute order: the same rule as child elements, below.
2. Then `[XmlText]` / `[XmlCData]` if simple content.
3. Then child elements.

Child element order ([K9](01-identity-and-decisions.md#key-decisions), [K36](01-identity-and-decisions.md#key-decisions)):

1. Members sort by order (stable), lowest first. The order is what the member sets (`[XmlPropertyOrder]`, else `[XmlElement.Order]`, else `[XmlArray.Order]`); a member that sets none is placed as one that sets `0`. So a negative order comes before the unordered members and a positive one after, as `[JsonPropertyOrder]` has it.
2. Members with the **same** order, which includes every member that sets none, keep **flattened declaration order**: most-base to most-derived; within a type, source declaration order (source-gen: `INamedTypeSymbol` declaration order; reflection: metadata token). Within a type the properties come before the fields; `MemberOrder = FieldsFirst` puts the fields first, as `System.Xml.Serialization` does ([decision 71](24-doc-findings-resolution.md)).

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

A collection member with no mapping attribute is unwrapped (repeated elements named for the member). `DefaultCollectionMapping = Wrapped` makes it wrapped, exactly as `[XmlArray]` with no arguments would: the wrapper is named for the member and the items for their type, and `[XmlArrayItem]` on the member names the items. `[XmlElement]`, typed or not, keeps a member unwrapped under either value ([decision 71](24-doc-findings-resolution.md)).

Supported collection types, matching STJ:

Every collection and dictionary that can be written can be read, with its items in document order:

- `T[]`, `List<T>`, and the interfaces a `List<T>` satisfies (`IList<T>`, `ICollection<T>`, `IEnumerable<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`), which read as `List<T>`
- `ISet<T>` and `IReadOnlySet<T>`, which read as `HashSet<T>`
- `Queue<T>`, `Stack<T>`, `LinkedList<T>`, `HashSet<T>`, `SortedSet<T>`, `Collection<T>`, `ObservableCollection<T>`, `ReadOnlyCollection<T>`, and the concurrent collections
- `ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableHashSet<T>`, `ImmutableSortedSet<T>`, `ImmutableQueue<T>`, `ImmutableStack<T>`, their interfaces, and `FrozenSet<T>`
- any other type with a public parameterless constructor and `ICollection<T>` or a public `Add` method, or a public constructor that takes `IEnumerable<T>` or `IList<T>`. A type with neither is written, and fails with `NotSupportedException` when it is read.
- `IAsyncEnumerable<T>` via `SerializeAsyncEnumerable` / `DeserializeAsyncEnumerable` only (not as a property type unless the member converter handles it)
- `Dictionary<TKey,TValue>`, `IDictionary<TKey,TValue>`, `IReadOnlyDictionary<TKey,TValue>`, `SortedDictionary<,>`, `SortedList<,>`, `ReadOnlyDictionary<,>`, `ConcurrentDictionary<,>`, `ImmutableDictionary<,>`, `ImmutableSortedDictionary<,>`, `IImmutableDictionary<,>` and `FrozenDictionary<,>` as **maps** ([K16](01-identity-and-decisions.md#key-decisions)). The two interfaces read as `Dictionary<,>`.

A stack is written from its top down and read back with the same top. A collection member with no setter is read only when it is populated (see "Object creation handling"). A null wrapped collection follows the ignore condition as any null member does, and a wrapper marked `xsi:nil` reads back as null. A null dictionary value is always an entry marked `xsi:nil`; the ignore condition is about members, and an entry is not one.

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

The key `Key` itself is written in the pair form, because an element named `Key` starts a pair when the document is read. A member declared as `IDictionary<TKey,TValue>` is read into a `Dictionary<TKey,TValue>`. A dictionary whose keys are not strings is always written as pairs, and reading one rejects an element that is not `Key` or `Value` with `XmlException`.

`[XmlArrayItem]` / `[XmlElement]` on a dictionary member can rename the entry wrapper. Null dictionary: omit or `xsi:nil` per nil/ignore rules.

**Not a map:** the `[XmlExtensionData]` member, whose type is `XmlExtensionData`.

**Default (unwrapped, STJ-like):** `List<string> Items` → repeated `<Items>`.

**Wrapped:** `[XmlArray("Items")]` `[XmlArrayItem("Item")]` → wrapper + item names.

`[XmlArrayItem]` names the items of a wrapped collection only. Without `[XmlArray]` it has no effect: the repeated elements have the member's name, which `[XmlElement("name")]` or `[XmlPropertyName]` changes.

Empty vs null:

| Value | Unwrapped | Wrapped |
| --- | --- | --- |
| `null`, ignore WhenWritingNull | omit | omit |
| `null`, nillable, Never | omit unless `[XmlArray]` | `<Items xsi:nil="true"/>` |
| empty | omit | `<Items/>` |

Unwrapped empty and unwrapped null both omit. Prefer wrapped collections or non-null empty lists.

## Object creation handling

`XmlSerializerOptions.PreferredObjectCreationHandling` and `[XmlObjectCreationHandling]` decide what happens to the value a member already holds when the member is read. The values and the rules are those of `JsonObjectCreationHandling` in System.Text.Json, taken from a probe of .NET 11.

- **`Replace` (default).** A member with a setter is given a new value. A member with no setter is not read, whatever its type.
- **`Populate`.** The items of a collection and the entries of a dictionary are added to the instance the member holds, and the members of an object are set on it, with or without a setter. An entry whose key is held already takes the value that was read. A member that holds null is given a new instance when it has a setter and is left null when it has none. A stack takes the items last to first, so the first item of the document ends on top.
- **Precedence.** The attribute on the member, then the attribute on the type, then the option. The attribute on a member applies to that member only, not to the members of the object it holds; the option reaches them because their type is bound with the same options.
- **What can be populated** is decided by the declared type. A collection: `ICollection<T>` and the interfaces that extend it, a class that implements it and has a public parameterless constructor, `Stack<T>`, `Queue<T>`. A dictionary: `IDictionary<TKey, TValue>`, or a class that implements it and has a public parameterless constructor. An object or a value type read by the built-in object converter; a value type needs a setter, because it is populated in its box and copied back. `Nullable<T>` follows `T`.
- **What cannot:** an array, a value-type collection or dictionary, an immutable or frozen one, a read-only interface (`IEnumerable<T>`, `IReadOnlyList<T>`, `IReadOnlyDictionary<,>`), mixed content, a polymorphic member, a value read by a converter the caller supplied, and any scalar. The option and the attribute on a type skip these. The attribute on such a member is `InvalidOperationException` when the type is bound: "Property 'X' on type 'T' is marked with XmlObjectCreationHandling.Populate but it doesn't support populating. This can be either because the property type is immutable or it could use a custom converter.", "… but is a value type that doesn't have a setter.", or "… but its type allows polymorphic deserialization."
- **Constructor with parameters.** A type created through one populates nothing. The attribute on it or on one of its members is `NotSupportedException`: "XmlObjectCreationHandling.Populate is currently not supported in types with parameterized constructors."
- **Reference handling.** A type with a member to populate and a `ReferenceHandler` is `InvalidOperationException`: "XmlObjectCreationHandling.Populate is incompatible with reference handling."
- **Nil.** An element marked `xsi:nil="true"` sets a populated member that has a setter to null. For one with no setter it is `InvalidOperationException`: "Unable to assign 'null' to the property or field of type 'T'."
- **A declared interface that holds a read-only instance** (an `IList<T>` holding an array) is `NotSupportedException` when it is populated: "The collection of type 'T' held by the member 'X' is read-only and cannot be populated." The dictionary message is "The dictionary of type 'T' is read-only and cannot be populated."
- **Where System.Text.Json differs.** It throws `InvalidCastException` when the option populates a polymorphic member that holds another derived type; here the option leaves a polymorphic member under `Replace`.
- **A generated context** follows the same rules, because the same engine reads from its metadata. It populates an object a member holds, an `init` member and a `required` member exactly as the reflection resolver does.

## Mixed content

`[XmlMixed]` on a collection of `XmlMixedContentItem` (a `List<XmlMixedContentItem>`, an array, or another supported collection of it) maps interleaved text and elements. A member of any other type is `NotSupportedException` when the type is bound ("Member '…' is marked [XmlMixedAttribute] but is not a collection of XmlMixedContentItem."), in the reflection resolver and in a generated context alike:

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

Text nodes become `XmlMixedText`. Every child element becomes an `XmlMixedElement`, including one named like the mixed member itself. A type with `[XmlMixed]` **and** ordinary element properties is `NotSupportedException` at type-info build. Simple content (`[XmlAttribute]` + `[XmlText]`) stays the leaf-element case.

## Polymorphism

```csharp
[XmlPolymorphic] // DiscriminatorPlacement = ElementName
[XmlDerivedType<Book>]
[XmlDerivedType<Dvd>]
public class Product
{
    public string Title { get; set; }
}
```

**Declared type.** A concrete class, an abstract class, or an interface. An abstract class or an interface must declare at least one `[XmlDerivedType]`; with none it is unsupported (`NotSupportedException`, "The type '…' is not supported."). It is never created: an element read as the declared type because nothing in it identifies a derived type is an `XmlException` ("The type '…' is abstract or an interface, so the element must identify one of its derived types."). Its type info lists its own members and has no `CreateObject`. Writing an undeclared derived type throws `NotSupportedException` unless `UnknownDerivedTypeHandling` falls back, in which case the declared type's own members are written.

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

An item whose runtime type is the declared type itself (a plain `Product` here) is written under the member name, `<Products>`, and read back as the declared type.

**A derived type as the declared type** ([decision 80](24-doc-findings-resolution.md)). A derived type inherits the list its base declares. Of that list, only the types that derive from the declared type are ones a value is written as: a `Book` declared as `Book` is written as itself, with no discriminator and under the name of its member, and a type below `Book` that `Product` lists is written with its discriminator or under its element name. This is what `System.Xml.Serialization` does. The whole list is used when a document is read: a discriminator or a document element that names the declared type is read as it, and one that names a type beside it (`Dvd` for a `Book`) is an `XmlException`. A member takes the element names of the types below its declared type only.

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
[XmlDerivedType<Book>("book")]
```

```xml
<Products kind="book">...</Products>
```

**XsiType placement:** writes `xsi:type="Book"` (our writer, not `System.Xml`). The value is read as a QName: a discriminator is matched as it is written first, and then by the local part after a prefix, so `xsi:type="p:Book"` is `Book` whatever prefix the document chose. The namespace the prefix is bound to is not checked.

Unknown derived types: `UnknownDerivedTypeHandling` (default `FailSerialization`).

Per-member `[XmlElement<Book>("Book")]` duplicates this for one property ([K35](01-identity-and-decisions.md#key-decisions)).

**Closed types.** `InferClosedTypePolymorphism`, on `XmlSerializerOptions` and on `[XmlPolymorphic]`, takes the derived types of a closed type from the list the compiler makes (`IsClosedTypeAttribute.DerivedTypes`). The rules are those of `JsonSerializerOptions.InferClosedTypePolymorphism` and `JsonPolymorphicAttribute.InferClosedTypePolymorphism`, taken from a probe of .NET 11:

- The option is `false` by default. The attribute is unset by default, which leaves it to the option; `true` infers for the type whatever the option says, and `false` turns it off for the type. The attribute is read from the type itself, not from a base type.
- A type that declares a derived type with `[XmlDerivedType]` has nothing inferred: what it declares is the whole list.
- The discriminator of an inferred derived type is its name, without the arity of a generic type. It goes through the element naming policy under `ElementName` placement and is used as it is under `Attribute` and `XsiType`, as a declared discriminator is.
- A derived type that is closed itself cannot be created, so its own derived types stand in its place, at any depth.
- A generic derived type is listed without type arguments and is given the arguments of the closed type. One that takes another number of arguments, cannot take these arguments, or does not derive from this closed form is left out. (System.Text.Json lists a derived type of another closed form; that is not followed.)
- The attribute set to `true` on a type that is not closed is `InvalidOperationException` when the type is bound: "The type 'T' enables XmlPolymorphicAttribute.InferClosedTypePolymorphism but is not a closed type, so no derived types can be inferred. Declare the type 'closed' or register its derived types using XmlDerivedTypeAttribute."
- A closed type is implicitly abstract. With no derived types declared and none inferred it is not supported (`NotSupportedException`, "The type 'T' is not supported."), in the reflection resolver and in a generated context alike.
- Closed types need C# 15 and .NET 11. In the `net10.0` build no type is closed, so the option has no type to act on and the attribute set to `true` is refused.
- A generated context lists the derived types of every closed type it reaches, because whether they are inferred is known only when it runs. A type that holds a closed type has no write fast path.

## Enums

- Default: write the member's C# **name** (`EnumHandling.Name`). No naming policy is applied to enum values, as in System.Text.Json, where only `JsonStringEnumConverter`'s own policy renames them.
- `[XmlPropertyName("foo")]` on the enum field overrides the name (`[JsonStringEnumMemberName]` in System.Text.Json). Matching on read is `JsonStringEnumConverter`'s: the exact name first; then a C# name without regard to case; a name given by the attribute replaces the C# name and is matched exactly; of two members with one name the first is read. `PropertyNameCaseInsensitive` has no part in it. A name that starts or ends with white space, or for `[Flags]` contains any, is `InvalidOperationException` when the type is first used.
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

The member must be of type `XmlExtensionData`; any other type is `NotSupportedException` when the type is first used ("Member 'T.M' is marked [XmlExtensionDataAttribute] but is not XmlExtensionData."). A dictionary keyed by local name is not accepted: it could hold neither the unmapped attributes nor two unmapped elements of one name. Unknown members go here when `UnmappedMemberHandling.Skip`. When `Disallow` and no extension bag, unknown elements/attributes throw. `xmlns*` attributes are never extension data.

## Namespaces on write

1. Declarations on the **document element**, in this order: `XmlSerializerOptions.Namespaces` (a prefix-to-URI dictionary, copied and checked when set), then each `[XmlNamespaceDeclaration("addr", "http://example.com/address")]` on the type whose prefix and namespace the options did not already declare. They are written whether or not they are used. A type that is a member of another type declares nothing. The element that holds an asynchronous sequence (`ArrayOfX`) is a document element; so is each top-level value.
2. `options.DefaultNamespace` / `[XmlRoot].Namespace` → the namespace of the document element. A member's `[XmlElement]` / `[XmlArray]` namespace: `null` (the default) names none, so the element takes the default namespace in scope; a URI puts it in that namespace; an **empty string** puts it in no namespace, written with `xmlns=""` where a default namespace is in scope, and then only an element in no namespace is read as the member ([decision 74](24-doc-findings-resolution.md)). A type with such a member has no write fast path.
3. The writer declares every other URI where it is first needed: as the default namespace of an element, or with a generated prefix (`ns1`) for an attribute. A URI that step 1 declared is in scope everywhere, so the writer uses its prefix; when two prefixes are bound to one URI, the one declared last.
4. How the document element is started when step 1 names its own namespace ([decision 64](24-doc-findings-resolution.md)): with no prefix when the empty prefix is bound to it; otherwise with the last prefix bound to it. In the second case no default namespace is in force, so a member with no namespace of its own is written in **no** namespace instead of inheriting the document element's. If the element's namespace has no declaration and the empty prefix is bound to another namespace, both would be `xmlns="..."` on one element: `InvalidOperationException` when the document is written.

`KnownNamespaces` holds ready-made dictionaries for step 1 (`Edmx`, `Podcast`, `Rss`, `Sitemap`, `Soap11`, `Soap12`, `Svg`, `XmlSchema`), and `XmlDocument.DiscoverNamespaces` reads them from an existing document ([DOM](06-api-dom.md)). Nothing here affects reading.

Name comparison for mapping is always `{local, ns}`, never prefix.

## Built-in primitives (serializer)

Same as reader getters, plus `string`, `char`, `Uri`, `Version`, `byte[]`, `Memory<byte>` and `ReadOnlyMemory<byte>` (base64), `Int128`, `UInt128` and `BigInteger` (decimal digits), `Half` (the lexical form of `xs:float`), and `enum`. A `char` that is white space is that character: white space is collapsed only around a character that is not white space. Every one of them is parsed by `XmlLexical`.

**XML values.** `XmlElement`, `XmlDocument`, `XmlNode` and `XmlElementNode` are XML already. As the document element the value is the document, under its own name. As a member it is the one element inside the member's element, which is what `System.Xml.Serialization` writes. Only an element can be the value. A value that is read holds no rented memory.

**`object`.** A value declared as `object` is written as the type it has: as the root under that type's element name, and as a member, a list item or a dictionary value inside the element that names it. A document does not record the type, so it reads back as XML. `UnknownTypeHandling` says which kind, as `JsonSerializerOptions.UnknownTypeHandling` chooses between `JsonElement` and `JsonNode`: `XmlElement` (default, read-only) or `XmlNode` (an `XmlElementNode`, which can be changed). It applies wherever the declared type is `object`: a member, a list item, a dictionary value and the root. A nil element is null under both. Generated contexts read `object` through the runtime library, so they follow it.

The element that is read is the one that names the value: the member's element, the item's, or the entry's (`<Any><b>1</b></Any>` reads as an element named `Any`). So an `XmlElement` or an `XmlElementNode` held in a member, a list item or a dictionary value declared as `object` is written as its attributes and its children inside that element, and what was read is written back as it was. The member, the item or the entry gives the name; the name of an element built under another name is not written, as the name of a type is not. A declaration of a prefix on the element is written. A declaration of the default namespace is not, because the element it would apply to is named already: a child in that namespace declares it for itself. As the root, the element is written as itself. A member declared as `XmlElement` or `XmlNode` is different: there the value is the element inside the member's element.

**Anonymous types** are all named `AnonymousType`, and are read through their constructor.

**A converter the caller supplies** (in `options.Converters`, or with `[XmlConverter]` on the type or on the member) makes its type a single value: it may be an attribute or the text of an element. `[XmlConverter]` on a member reads and writes that member only.

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
| `[XmlInclude(typeof(T))]` | `[XmlDerivedType<T>]` |
| `[XmlEnum("n")]` | `[XmlPropertyName("n")]` on the field |
| `[XmlText]` | `[XmlText]` |
| a collection with no setter is filled | not read under the default `Replace`; `[XmlObjectCreationHandling(Populate)]` or `PreferredObjectCreationHandling = Populate` fills it |
| `[XmlType]` | none: `[XmlRoot]` names the document element, and the argument of `[XmlDerivedType<T>]` a derived type |
| public types only | internal OK with source-gen (consuming compilation) |
| base-then-derived Order | default same; `[XmlPropertyOrder]` may interleave |
| sgen sidecar | `[XmlSerializable]` context |

---

[Index](README.md) · [Previous: Converters and attributes](08-api-converters-and-attributes.md) · [Next: Source generation](10-source-generation.md)
