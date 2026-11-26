# Typed first

Part of the [XmlEssentials spec](README.md).

This file is the rule that public and internal code is generic wherever it can be, the list of places where the library is not generic today, and the plan for moving them. It was set by the owner on 2026-11-26 while the [contract modifiers](27-modifiers.md) were designed. Three changes are done; the task list is at the end.

## Why

XmlEssentials is shaped like System.Text.Json, and System.Text.Json carries shapes from an older runtime and an older design: accessors that take and return `object`, a factory declared once as `Func<object>` and again as `Func<T>`, callbacks that are `Action<object>`, and a set of serializer overloads that take a `Type` beside every generic one. Each of those boxes a value, costs a cast in the caller's code, or moves a check from the compiler to run time.

The library is a clone of System.Text.Json in capability, not in every signature ([contract modifiers](27-modifiers.md#direction), decision 1). Where System.Text.Json has an untyped shape for a reason that no longer holds, this library takes the typed one.

## The rule

**A member that works with a type is generic over it.** This holds for public API and for internals. A member that takes or returns a `Type`, an `object`, an untyped `XmlTypeInfo` or an untyped delegate is allowed only for one of the reasons below, and the reason is written beside it: in the XML documentation of a public member, in a comment on an internal one.

| # | Reason | What it allows | Example |
| --- | --- | --- | --- |
| 1 | **Different types in one place.** A container, a cache or a callback handles values whose type arguments differ. | An untyped base class with a typed class under it. The base holds only what does not depend on the type argument. | `XmlTypeInfo` under the options cache; `XmlPropertyInfo` in `Properties`; a modifier that runs for every type |
| 2 | **One factory for an open generic.** Code written once serves `List<T>` for every `T`. | A method that takes a `Type` and returns the untyped base. | `XmlConverterFactory.CreateConverter(Type, XmlSerializerOptions)` |
| 3 | **A description.** The member says what a type is and nothing is called through it. | A `Type` property, read-only. | `XmlPropertyInfo.PropertyType`, `XmlTypeInfo.ElementType`, `XmlParameterInfo.ParameterType` |
| 4 | **The caller has only a `Type`.** A web formatter is handed `object` and `Type`; a message bus reads a type name and has to produce an object. | One door, on the untyped base. Not a parallel set of overloads. | See [the door](#the-door-for-a-caller-that-has-only-a-type) |

What the rule does not ask for:

- **Not everything is generic.** A setting such as `IsAttribute` or `Order` has no type argument to take.
- **No untyped twin.** When a typed member exists, there is no untyped member beside it that does the same thing. A caller that holds the untyped base tests for the typed class (`if (info is XmlTypeInfo<Order> order)`).
- **No reflection to get there.** A generic member that can only be reached through `MakeGenericType` or `MakeGenericMethod` is not usable from a source-generated context. Where a constraint stands in the way, a helper that carries the constraint closes it at compile time, as `XmlMetadataServices.CreateStructPropertyInfo` does.

## What is typed already

| Area | State |
| --- | --- |
| `XmlSerializer.Serialize<TValue>` and `Deserialize<TValue>`, with options, with `XmlTypeInfo<TValue>`, with a context | Shipped |
| `XmlConverter<T>`, `XmlTypeInfo<T>`, `XmlObjectInfoValues<T>` | Shipped |
| Attributes that name a type: `[XmlElement<T>]`, `[XmlArrayItem<T>]`, `[XmlDerivedType<T>]`, `[XmlConverter<T>]`, `[XmlSerializable<T>]` | Shipped |
| `XmlMetadataServices.CreateValueInfo<T>(options, XmlConverter<T>)`, where System.Text.Json takes an untyped converter | Shipped |
| `XmlTypeInfo<T>.CreateObject` is a `Func<T>`; the untyped base has none | **Done** 2026-11-26 ([contract modifiers](27-modifiers.md), B1) |
| `XmlPropertyInfoValues<TDeclaring, TValue>`, with a `Func<TDeclaring, TValue>` getter and an `XmlPropertySetter<TDeclaring, TValue>` setter | **Done** 2026-11-26 (B2) |
| `XmlBoundProperty<TDeclaring, TValue>` and the extension-data binding hold those delegates, through `XmlMemberAccess` | **Done** 2026-11-26 (B2) |

## What is not typed, and what happens to it

Found by a search of the public declarations of `CloudNimble.XmlEssentials` for a `Type`, an `object` or an untyped `XmlTypeInfo` in a signature, and by reading the object engine. The counts are from that search and were not checked overload by overload.

### Public

| Member | Today | Target | Reason it is untyped, or the work |
| --- | --- | --- | --- |
| `XmlSerializer` overloads that take a `Type`, or a context with a `Type` | About 38: 20 in `XmlSerializer.Deserialize.cs`, 18 in `XmlSerializer.Serialize.cs` | Stay ([contract modifiers](27-modifiers.md#direction), decision 3). Nothing new is added beside them, and nothing in the library calls them where the type is known (task 2). | Reason 4. They are more than one door; whether they go is the owner's decision, after task 2 shows which have a caller. |
| The contract: `XmlTypeInfo`, `XmlPropertyInfo`, modifiers, derived types, callbacks | Read-only and mostly untyped | The [public API](27-modifiers.md#public-api) of contract modifiers | That spec |
| `IXmlTypeInfoResolver.GetTypeInfo(Type, XmlSerializerOptions)`, `XmlSerializerOptions.GetTypeInfo(Type)` | Untyped | Stay. `GetTypeInfo<T>()` is what code that knows the type calls. | Reason 1: the cache behind them holds every type. A generic method on the interface would have to be closed over a run-time `Type` by the reflection resolver. |
| `XmlSerializerOptions.GetConverter(Type)` | Untyped, and there is no generic one | Add `GetConverter<T>()`, which returns an `XmlConverter<T>`. The untyped one stays. | Reason 1 for the untyped one |
| `XmlConverter`, `XmlConverterFactory`, `CanConvert(Type)`, `CreateConverter(Type, options)` | Untyped | Stay | Reasons 1 and 2 |
| `XmlConverter<T>.Read(ref reader, Type typeToConvert, options)` | Takes the type beside `T` | Stay | Reason 3 in effect: a converter for `T` may be registered for a type that derives from `T`, and is told which. System.Text.Json has the same parameter. |
| `XmlReferenceResolver` (`AddReference(string, object)`, `ResolveReference`, `GetReference(object, …)`) | `object` | Stay | Reason 1: one resolver holds references of every type |
| `XmlObjectInfoValues<T>.ObjectWithParameterizedConstructorCreator` | `Func<object?[], T>` | Stay | Reason 1: the arguments of a constructor have different types |
| `XmlParameterInfoValues` (`ParameterType`, `DefaultValue`) | `Type`, `object` | Stay | Reason 3 |
| `XmlNamedValue.Value` | `object` | To review: a typed `XmlNamedValue<T>` beside or in place of it | Not decided. Read what the choice members that use it need first. |
| `XmlDerivedTypeAttribute.TypeDiscriminator` | `object?`, as System.Text.Json has it for a string or an integer | To review: an XML discriminator is a name, so a `string` may be the whole range | Not decided |

### Internal

| Place | Today | Target | Note |
| --- | --- | --- | --- |
| The library's own calls to the `XmlSerializer` overloads that take a `Type` or an untyped type info | Not counted yet | The generic overload wherever the type is known | Task 2. No public change. |
| The reflection resolver's members | Closed over the type that is bound (`XmlObjectBinder.CreateFacts<T>`), which may derive from the type that declares the member | Closed over the declaring type, as generated metadata is | Needed before `XmlPropertyInfo<TDeclaring, TValue>` is public, so that one member has one type whichever resolver made it |
| `XmlMemberFacts.Getter`, `Setter`, `Unbox` | `Delegate`, cast where the member is bound | Typed facts, generic over the declaring type and the member's type, under an untyped base for the binder's lists | Reason 1 for the base |
| `XmlObjectBinding.CreateObject` | `Func<object>`. `XmlObjectTypeInfo<T>.CreateObject` wraps it in a `Func<T>` | One `Func<T>`, held by the typed type info and called by the typed converter | The wrapper is a leftover of B1 |
| The object being read (`state.Instance`, the `object instance` parameters of `XmlBoundProperty`) | `object`, so a value type is in a box | To measure before deciding: a read state that is generic over the type | The largest change here. `XmlResumableReader` keeps the state across reads, and the bound members are called through an untyped base. Nothing is claimed about speed until the final benchmark pass. |
| Values held for a constructor | `object?[]`, with a reusable box for a value type | Stay | Reason 1 |
| Collection builders (`PopulateCollection(object builder, …)`, `XmlCollectionBinding`) | `object` | To read first; a generic `XmlCollectionBinding` exists (`XmlCollectionBindingOfT.cs`) and may already hold the typed half | Not read for this spec |
| Polymorphic dispatch: the table from `value.GetType()` to a derived type's writer | Untyped | Stay | Reason 1 |

## The door for a caller that has only a `Type`

A caller under reason 4 needs three things: the type info for a `Type`, a write that takes an `object`, and a read that returns one. The first exists (`XmlSerializerOptions.GetTypeInfo(Type)`). The other two are proposed as members of the untyped `XmlTypeInfo`:

```csharp
public abstract class XmlTypeInfo
{
    public void Serialize(Utf8XmlWriter writer, object? value);
    public object? Deserialize(ref Utf8XmlReader reader);
}
```

- They are the only untyped read and write the library would need to offer. The stream, string, document and asynchronous forms are built on them by the caller, or stay as the shipped `Type` overloads for as long as those stay.
- They carry no trimming or Native AOT requirement of their own: the type info was resolved before the call.
- **Not decided.** The names, whether asynchronous forms belong here, and whether the shipped overloads are then marked obsolete are the owner's to settle after task 2.

## How a new member is checked

- **In review.** A new member with a `Type`, an `object`, an untyped `XmlTypeInfo` or an untyped delegate in its signature states its reason, by number.
- **By a test.** `XmlTypedSurfaceTests` lists every public member of the runtime library whose signature has one of those, by reflection, and compares the list with one in the test, each entry with its reason. A new untyped member fails the test until it is added with a reason, which is the moment someone has to say why it cannot be generic. The list starts as what exists on the day the test is written.

## Tasks

| # | Task | Status |
| --- | --- | --- |
| 1 | **The contract, typed.** The [public API](27-modifiers.md#public-api) of contract modifiers, with that spec's tasks. | Designed |
| 2 | **Move the library's own callers.** Every call inside `/src` to an `XmlSerializer` overload that takes a `Type`, an untyped `XmlTypeInfo` or a context, replaced by the generic one where the type is known. Record which overloads keep a caller, and why. Samples and guides show the generic overloads first. | Not started |
| 3 | **`XmlTypedSurfaceTests`.** The list of untyped public members with their reasons, as above. | Not started |
| 4 | **The reflection resolver closes members over the declaring type.** | Not started |
| 5 | **Typed member facts.** `XmlMemberFacts` holds typed delegates; no `Delegate` field is cast. | Not started |
| 6 | **One `CreateObject`.** `XmlObjectBinding` holds a `Func<T>`; the wrapper in `XmlObjectTypeInfo<T>` goes. | Not started |
| 7 | **Add `XmlSerializerOptions.GetConverter<T>()`. Review `XmlNamedValue.Value`, `XmlDerivedTypeAttribute.TypeDiscriminator` and the collection builders;** each ends as a row in the tables above with a decision. | Not started |
| 8 | **The door.** `XmlTypeInfo.Serialize` and `Deserialize`, if the owner wants them, and the decision about the shipped overloads. | Waiting on the owner, after task 2 |
| 9 | **A typed read state.** Measure first: a profile of object reads that shows what the box of a value type and the casts cost. Change it only if the profile says so. | In the final benchmark pass |
| 10 | **Docs.** The guides lead with the generic overloads; `from-system-text-json.mdx` says where the shapes differ and why. | Not started |

Tasks 4 to 6 change no public member and no output. Each is checked by the whole suite and the coverage gate, unchanged.

## Rules that stay in force

- **No trimming or Native AOT warning is suppressed** to make something generic ([AOT](12-aot-and-trimming.md)). If a typed shape needs one, the shape is wrong.
- **No object-layout trick.** Reaching into a box goes through `Unsafe.Unbox<T>` in code that is constrained to value types.
- **A removal of shipped surface is the owner's decision**, one at a time ([contract modifiers](27-modifiers.md#direction), decision 3).
- **No claim about speed** without a benchmark, and benchmarks run in the final pass ([optimization](23-optimization.md)). The three unmeasured costs of B2 are listed in [contract modifiers](27-modifiers.md#b2-the-design).

## Progress notes

Add a dated line here whenever a task finishes or a decision changes.

- 2026-11-26: the benchmark pass ([optimization](23-optimization.md)) found no cost from B1 to B3 for classes or for value types through generated metadata, and one regression on the reflection path (a field of a value type), which is fixed. `XmlSerializerValueTypeBenchmarks` is new, so a value type is in the suite from here on.
- 2026-11-26: written. Done before it was written, under [contract modifiers](27-modifiers.md): `CreateObject` typed (B1), member metadata typed by the declaring type (B2), and the bound members holding typed delegates. The inventory was made by a pattern search of public signatures and a reading of `XmlObjectBinder`, `XmlBoundProperty<TDeclaring, TValue>`, `XmlMemberFacts`, `XmlObjectBinding` and `XmlObjectTypeInfo<T>`. Not read: the collection bindings, `XmlResumableReader`, the polymorphism tables and `XmlNamedValue`; their rows say so.

---

[Index](README.md) · [Previous: Contract modifiers](27-modifiers.md)
