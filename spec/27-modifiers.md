# Contract modifiers

Part of the [XmlEssentials spec](README.md).

This file is the design for **modifiers**: callbacks that change the contract of a type after the serializer has built it and before it is used. They are what `DefaultJsonTypeInfoResolver.Modifiers` and `JsonTypeInfoResolver.WithAddedModifier` are in System.Text.Json. Only the first task is done (`PropertyType`). The task list is at the end.

## Direction

Three decisions by the owner on 2026-11-26 govern this file. Where an older section disagrees with them, they win, and the section is marked.

1. **A clone in capability, not in every signature.** Every capability of System.Text.Json has a counterpart here, in the same shape and vocabulary. Where a System.Text.Json shape is a compromise of its history (accessors that box, one member declared twice, a second way to build a contract for the generator, obsolete members), this library takes the better shape. Leaving a capability out is the failure.
2. **Typed first.** Whatever has to be public, and as much of the internals as possible, is generic. A member that works with types and is not generic needs a stated reason. The reasons accepted so far: a container whose entries have different type arguments, and one factory that serves an open generic. A caller that has only a run-time `Type` is a narrow case (a web formatter, a message bus), served by one door, not by a parallel API.
3. **Shipped surface stays for now.** The `XmlSerializer` overloads that take a `Type`, an untyped `XmlTypeInfo` or a context are not removed. New surface is generic. The library's own code moves to the generic path and stops calling those overloads where it can, which shows which of them are extraneous. Removing them is a later decision.

The [public API](#public-api) block is written to these decisions. How the rest of the library moves to them is [typed first](28-typed-first.md).

## Why

Every mapping rule is set today with an attribute on a type or a member, or with an option that applies to every type. Neither reaches a type the caller does not own:

- A model assembly that must not reference XmlEssentials cannot carry `[XmlAttribute]`, `[XmlPropertyName]` or `[XmlRoot]`. The caller's only route is a derived class per type that redeclares the members, or a converter per type.
- One member cannot be read from two names (a legacy element name, or an attribute and an element of one name). `[XmlAttribute]` with `[XmlElement]` on one member is refused (`XmlObjectBinder.TryCreateModel`).
- `DefaultPropertyMapping` is one value for the whole options instance. A graph in which some types are written as attributes and the rest as elements needs an attribute on every member of the first kind.

A modifier does all three from outside the type. The case that prompted this is DotNetDocs, whose template models live in `Mintlify.Core`, which stays free of XML attributes.

## What System.Text.Json does

Taken from a probe on .NET 11.0.0 on 2026-11-25 (reflection over the public surface and a run of each behaviour), not from memory.

- `DefaultJsonTypeInfoResolver` has a public constructor and `IList<Action<JsonTypeInfo>> Modifiers { get; }`. A `null` modifier is an `ArgumentNullException`.
- The resolver builds the `JsonTypeInfo`, calls each modifier in list order, and returns it. The modifiers see every type the options ask for: `P` (`Kind` `Object`), `Int32` (`None`), `List<int>` (`Enumerable`).
- After the resolver has been used, `Modifiers.Add` throws `InvalidOperationException`: "Cannot add callbacks to the 'Modifiers' property after the resolver has been used for the first time."
- A type info handed out by the options is read-only. A setter, or `Properties.Add`, throws `InvalidOperationException`: "This JsonTypeInfo instance is marked read-only or has already been used in serialization or deserialization."
- `resolver.GetTypeInfo(type, options)` called directly returns a new instance each time, with the modifiers applied and `IsReadOnly == false`.
- An exception a modifier throws reaches the caller unchanged (a `FormatException` stayed a `FormatException`).
- Two properties with one name: `InvalidOperationException`, "The JSON property name for 'P.L' collides with another property."
- On a type info whose `Kind` is not `Object`, `CreateJsonPropertyInfo` succeeds and `Properties.Add` throws `InvalidOperationException`: "Invalid JsonTypeInfo operation for JsonTypeInfoKind 'None'." Setting `CreateObject` throws the same for `None` and is accepted for `Enumerable` and `Dictionary`.
- `[JsonIgnore]` arrives in the contract as accessors, not as a condition. `Always`: the property is in `Properties` with `Get` and `Set` both `null`. `WhenReading`, and a property with no setter: `Set` is `null`. `WhenWriting`, `WhenWritingNull` and `WhenWritingDefault`: `Get` and `Set` are both there and `ShouldSerialize` is set. A plain property has a `null` `ShouldSerialize`.
- A modifier that asks the options for the type it is modifying (`ti.Options.GetTypeInfo(ti.Type)`) runs again inside itself, without end: the probe stopped it at the fourth level. System.Text.Json does not guard this.
- A property with no `Get` is not written. A property added with `CreateJsonPropertyInfo` and given only a `Set` is read and never written: `{"alias":5}` set `P.X`, and `P` was written back as `{"X":5,"L":[1]}`.
- `JsonTypeInfoResolver.WithAddedModifier(this IJsonTypeInfoResolver, Action<JsonTypeInfo>)` returns a resolver of a non-public type. Over a source-generated context, a modifier that renames a property is honoured: the output has the new name, so the generated write fast path did not write it.
- `CreateJsonPropertyInfo(Type, string)` carries `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`.
- Settable on `JsonTypeInfo`: `CreateObject`, `OnSerializing`, `OnSerialized`, `OnDeserializing`, `OnDeserialized`, `PolymorphismOptions`, `NumberHandling`, `UnmappedMemberHandling`, `PreferredPropertyObjectCreationHandling`, `OriginatingResolver`. Read-only: `Type`, `Converter`, `Kind`, `Options`, `IsReadOnly`, `ElementType`, `KeyType`, `ConstructorAttributeProvider`.
- Settable on `JsonPropertyInfo`: `Name`, `Order`, `Get`, `Set`, `ShouldSerialize`, `CustomConverter`, `AttributeProvider`, `ObjectCreationHandling`, `IsGetNullable`, `IsSetNullable`, `IsExtensionData`, `IsRequired`, `NumberHandling`. Read-only: `PropertyType` (a `Type`), `DeclaringType`, `Options`, `AssociatedParameter`.
- One difference that is not copied: two `JsonSerializerOptions` with the same resolver and equal settings ran a modifier once between them, and a third with `WriteIndented` set ran it again. XmlEssentials caches type info per options instance, so a modifier runs once per type **per options instance**.

## The rule that governs the surface

**Every settable member of the contract has a counterpart that exists today, an attribute or an option, and setting it gives exactly what the counterpart gives.** A modifier that sets `IsAttribute` gets what `[XmlAttribute]` gets, including its refusals and their messages. The engine has one set of rules; a modifier is a third way to supply the facts they work from, after reflection and generated metadata ([single engine](25-single-engine-metadata.md)).

So this design adds no mapping feature, with one exception: the four callbacks, which have no attribute or interface today and are in System.Text.Json's contract. They are a separate task.

`NumberHandling` and `UnmappedMemberHandling` are options only today. System.Text.Json has both per type and per member, so both are owed ([Direction](#direction), decision 1): the attributes and the engine support come with the contract members.

## Public API

Typed first ([Direction](#direction), decision 2, and [typed first](28-typed-first.md)). A member is generic unless one of the reasons in that spec applies, and the comment beside each untyped member says which. Members marked **done** exist today.

```csharp
namespace CloudNimble.Text.Xml.Serialization;

public sealed class DefaultXmlTypeInfoResolver : IXmlTypeInfoResolver     // [RequiresUnreferencedCode], [RequiresDynamicCode], as today
{
    public DefaultXmlTypeInfoResolver();                                  // was internal
    public static DefaultXmlTypeInfoResolver Instance { get; }

    // A modifier for one type, typed. This is the form to reach for.
    public DefaultXmlTypeInfoResolver AddModifier<T>(Action<XmlTypeInfo<T>> modifier);

    // Modifiers that run for every type. Untyped: one delegate is called for types it cannot name (reason 1),
    // and C# has no generic lambda. AddModifier<T> adds to this list.
    public IList<Action<XmlTypeInfo>> Modifiers { get; }

    // Untyped: asked with a type known only at run time, and answered from a cache of mixed types (reason 1).
    public XmlTypeInfo? GetTypeInfo(Type type, XmlSerializerOptions options);
}

public static class XmlTypeInfoResolver
{
    public static IXmlTypeInfoResolver Combine(params IXmlTypeInfoResolver?[] resolvers);
    public static IXmlTypeInfoResolver Combine(params ReadOnlySpan<IXmlTypeInfoResolver?> resolvers);
    public static IXmlTypeInfoResolver WithAddedModifier<T>(this IXmlTypeInfoResolver resolver, Action<XmlTypeInfo<T>> modifier);
    public static IXmlTypeInfoResolver WithAddedModifier(this IXmlTypeInfoResolver resolver, Action<XmlTypeInfo> modifier);   // every type
}

public enum XmlTypeInfoKind
{
    None = 0,          // read and written by a converter: a scalar, an XML value, a type with a converter of the caller's
    Object = 1,
    Enumerable = 2,
    Dictionary = 3,
}

// The untyped base holds what does not depend on T. It exists because the options cache, a resolver and a
// modifier for every type hold type infos of different types (reason 1).
public abstract class XmlTypeInfo
{
    public static XmlTypeInfo<T> Create<T>(XmlSerializerOptions options); // a blank contract, for a hand-written resolver

    public Type Type { get; }
    public XmlTypeInfoKind Kind { get; }
    public XmlSerializerOptions Options { get; }
    public XmlConverter Converter { get; }                                // untyped view of XmlTypeInfo<T>.Converter (reason 1)
    public bool IsNillable { get; }
    public bool IsReadOnly { get; }
    public void MakeReadOnly();
    public IXmlTypeInfoResolver? OriginatingResolver { get; set; }

    public XmlName RootName { get; set; }                                 // was get-only
    public IList<XmlPropertyInfo> Properties { get; }                     // members of different types (reason 1); done as a read-only list
    public XmlPropertyInfo<TDeclaring, TValue>? GetProperty<TDeclaring, TValue>(string memberName);
    public XmlPolymorphismOptions? PolymorphismOptions { get; set; }      // derived types are of different types (reason 1)
    public XmlObjectCreationHandling? PreferredPropertyObjectCreationHandling { get; set; }
    public XmlNumberHandling? NumberHandling { get; set; }
    public XmlUnmappedMemberHandling? UnmappedMemberHandling { get; set; }

    // Facts about the type, read-only. A Type, as PropertyType is: nothing is called through them (reason 3).
    public Type? ElementType { get; }                                     // the item type of an Enumerable or a Dictionary
    public Type? KeyType { get; }                                         // the key type of a Dictionary
    public ICustomAttributeProvider? ConstructorAttributeProvider { get; }
}

public abstract class XmlTypeInfo<T> : XmlTypeInfo
{
    public new XmlConverter<T> Converter { get; }
    public Func<T>? CreateObject { get; set; }                            // done, get-only; on this class only (B1)

    public XmlPropertyInfo<T, TValue> CreateProperty<TValue>(string name);
    public XmlPropertyInfo<T, TValue>? GetProperty<TValue>(string memberName);   // a member T declares
    public void AddDerivedType<TDerived>(string? typeDiscriminator = null, string? @namespace = null) where TDerived : T;

    public Action<T>? OnSerializing { get; set; }
    public Action<T>? OnSerialized { get; set; }
    public Action<T>? OnDeserializing { get; set; }
    public Action<T>? OnDeserialized { get; set; }
}

// The untyped base holds what does not depend on the member's type. It exists because Properties holds members
// of different types (reason 1). It has no accessors.
public abstract class XmlPropertyInfo                                     // was sealed
{
    public Type DeclaringType { get; }
    public Type PropertyType { get; }                                     // done; was XmlTypeInfo
    public string? MemberName { get; }                                    // the C# name; null for a property a modifier created
    public XmlSerializerOptions Options { get; }
    public XmlParameterInfo? AssociatedParameter { get; }
    public ICustomAttributeProvider? AttributeProvider { get; set; }

    public XmlName Name { get; set; }
    public bool IsUnqualified { get; set; }
    public int? Order { get; set; }                                       // done, get-only; null when the member sets none (B3)
    public bool IsAttribute { get; set; }
    public bool IsText { get; set; }
    public bool IsCData { get; set; }
    public bool IsMixed { get; }
    public bool IsScalar { get; }
    public bool IsNillable { get; set; }
    public bool IsRequired { get; set; }
    public bool IsExtensionData { get; set; }
    public bool IsGetNullable { get; set; }
    public bool IsSetNullable { get; set; }
    public bool HasGetter { get; }                                        // whether the typed class has a Get, without naming its types
    public bool HasSetter { get; }
    public XmlObjectCreationHandling? ObjectCreationHandling { get; set; }
    public XmlNumberHandling? NumberHandling { get; set; }

    // Untyped: a converter factory serves an open generic, and a converter for T serves a member of type T? (reason 2).
    public XmlConverter? CustomConverter { get; set; }

    public XmlCollectionMapping? CollectionMapping { get; set; }          // null when the member is not a collection
    public XmlName? ItemName { get; set; }
    public IList<XmlElementChoice> ElementChoices { get; }                // choices are of different types (reason 1)
}

public sealed class XmlPropertyInfo<TDeclaring, TValue> : XmlPropertyInfo
{
    public Func<TDeclaring, TValue>? Get { get; set; }
    public XmlPropertySetter<TDeclaring, TValue>? Set { get; set; }       // the delegate of B2: by reference, so it sets a struct member
    public Func<TDeclaring, TValue, bool>? ShouldSerialize { get; set; }
}

public sealed class XmlElementChoice                                      // was internal
{
    public static XmlElementChoice Create<T>(string? elementName = null, string? @namespace = null);
    public Type Type { get; }
    public string? ElementName { get; }
    public string? Namespace { get; }
}

public sealed class XmlPolymorphismOptions
{
    public XmlPolymorphismOptions();
    public IList<XmlDerivedType> DerivedTypes { get; }
    public XmlDiscriminatorPlacement DiscriminatorPlacement { get; set; }
    public string? TypeDiscriminatorPropertyName { get; set; }
    public XmlUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get; set; }
    public bool? InferClosedTypePolymorphism { get; set; }               // null: the option decides, as an attribute that does not set it
}

public readonly struct XmlDerivedType
{
    // The only way to make one. The compiler checks that TDerived derives from TBase, which System.Text.Json
    // checks when the type is first used. There is no constructor that takes a Type.
    public static XmlDerivedType Create<TBase, TDerived>(string? typeDiscriminator = null, string? @namespace = null) where TDerived : TBase;
    public Type BaseType { get; }
    public Type DerivedType { get; }
    public string? TypeDiscriminator { get; }
    public string? Namespace { get; }
}

// A description of a constructor parameter, read-only. Untyped: nothing is called through it (reason 3).
public sealed class XmlParameterInfo
{
    public Type DeclaringType { get; }
    public string Name { get; }
    public Type ParameterType { get; }
    public int Position { get; }
    public bool HasDefaultValue { get; }
    public object? DefaultValue { get; }
    public bool IsNullable { get; }
}

public sealed class XmlPropertyInfoValues<TDeclaring, TValue> : XmlPropertyInfoValues
{
    // added to what single engine lists
    public Func<ICustomAttributeProvider>? AttributeProviderFactory { get; init; }
}
```

Where this departs from System.Text.Json's shape, and why:

| System.Text.Json | Here | Why |
| --- | --- | --- |
| `JsonPropertyInfo.Get`, `Set`, `ShouldSerialize` take and return `object` | On `XmlPropertyInfo<TDeclaring, TValue>`, typed | No boxing, no cast in the caller's code, and they are the delegates the engine calls |
| `JsonTypeInfo.CreateObject` as `Func<object>`, shadowed by `Func<T>` | `Func<T>` on `XmlTypeInfo<T>` only | One member for one thing |
| The four callbacks as `Action<object>` on `JsonTypeInfo` | `Action<T>` on `XmlTypeInfo<T>` | The callback is written for a type |
| `CreateJsonPropertyInfo(Type, string)` | `CreateProperty<TValue>(string)` only | Closing a member over a run-time `Type` needs dynamic code |
| `JsonDerivedType(Type, …)` | `XmlDerivedType.Create<TBase, TDerived>(…)`, and `AddDerivedType<TDerived>` | The compiler checks the relationship |
| A modifier is an `Action<JsonTypeInfo>` only | `AddModifier<T>` and `WithAddedModifier<T>` beside it | A modifier is usually written for one type |
| No lookup; a modifier loops over `Properties` | `GetProperty<TValue>(memberName)` | It returns the typed property, so the accessors can be set without a cast |
| `JsonTypeInfo.CreateJsonTypeInfo(Type, options)` and the generic one | `XmlTypeInfo.Create<T>(options)` only | A resolver that is asked with a `Type` answers from the types it was written for |

### Counterparts

| Member | Is what this gives | Notes |
| --- | --- | --- |
| `XmlTypeInfo.RootName` | `[XmlRoot]` | The name of the type wherever `[XmlRoot]` would give it: the document element, an item that is named for its type, and the `ArrayOf…` name of a sequence. |
| `XmlTypeInfo<T>.CreateObject` | The parameterless constructor | Setting it on a type created through a constructor with parameters makes it a type created with no arguments: every member is then set after creation, and one that cannot be set is not read. |
| `XmlTypeInfo.PolymorphismOptions` | `[XmlPolymorphic]` and `[XmlDerivedType<T>]` | `null` when the type declares none. Replacing it replaces the whole list. |
| `XmlTypeInfo.PreferredPropertyObjectCreationHandling` | `[XmlObjectCreationHandling]` on the type | `null` means the option decides. |
| `XmlPropertyInfo.Name` | `[XmlPropertyName]`, or the name and `Namespace` of `[XmlElement]` / `[XmlAttribute]` | A name a modifier sets is not passed through a naming policy, as a name an attribute gives is not. |
| `IsUnqualified` | `Namespace = ""` | "In no namespace, exactly" ([decision 74](24-doc-findings-resolution.md)). `XmlName` cannot say it: its namespace is never `null`. |
| `Order` | `[XmlPropertyOrder]` | `null` is unset and is placed as `0` (K36). The position in `Properties` is the declaration order that breaks a tie. |
| `IsAttribute`, `IsText`, `IsCData` | `[XmlAttribute]`, `[XmlText]`, `[XmlCData]` | All `false` is an element. |
| `IsMixed` | `[XmlMixed]` | Read-only: the attribute also fixes the type of the member. |
| `IsScalar` | The test `DefaultPropertyMapping.Attribute` makes | Read-only. `true` when the member's type is a built-in scalar (`XmlBuiltInConverters.IsScalar`), which is the only kind of member that option maps as an attribute. It is answered from the type alone, so a modifier need not resolve anything to ask it. |
| `IsNillable` | `XmlElementAttribute.IsNullable` | |
| `IsRequired` | `[XmlRequired]`, the `required` modifier | |
| `IsExtensionData` | `[XmlExtensionData]` | The member is in `Properties`. `PropertyType` must be `XmlExtensionData`. |
| `IsGetNullable`, `IsSetNullable` | The nullable annotations | Read only under `RespectNullableAnnotations`. |
| `Get`, `Set` (on `XmlPropertyInfo<TDeclaring, TValue>`; `HasGetter`, `HasSetter` on the base) | The accessors, and `[XmlIgnore]` | `Get == null`: never written. `Set == null`: never read, unless it fills a constructor parameter or is populated. `XmlIgnoreCondition.WhenReading` is a `null` `Set`. `Always` is a property that stays in `Properties` with both `null`, so a modifier can give an ignored member back its accessors. |
| `ShouldSerialize` (typed) | `[XmlIgnore(Condition = …)]`, `DefaultIgnoreCondition` | Called with the object and the value, both typed; `false` leaves the member out. `null` always writes. `WhenWriting`, `WhenWritingNull`, `WhenWritingDefault` and `WhenWritingNullReference` are each a delegate here. |
| `CustomConverter` | `[XmlConverter<T>]` on the member | |
| `ObjectCreationHandling` | `[XmlObjectCreationHandling]` on the member | |
| `CollectionMapping` | `[XmlArray]` (`Wrapped`) or `[XmlElement]` on a collection (`Unwrapped`), `DefaultCollectionMapping` | Setting it on a member that is not a collection is refused when the type info is made read-only. |
| `ItemName` | `[XmlArrayItem]` | |
| `ElementChoices` | `[XmlElement<T>]` more than once, `[XmlArrayItem<T>]` more than once | Empty for a member with no choice. A choice is made with `XmlElementChoice.Create<T>`. |
| `NumberHandling` (type and member) | `[XmlNumberHandling]`, which is owed with it | `null` means the option decides. |
| `UnmappedMemberHandling` | `[XmlUnmappedMemberHandling]`, which is owed with it | `null` means the option decides. |
| `XmlTypeInfo<T>.AddDerivedType<TDerived>` | `[XmlDerivedType<TDerived>]` | Creates `PolymorphismOptions` when the type has none. |

The ignore condition is not a property of its own. It arrives as `Get`, `Set` and `ShouldSerialize`, in the shapes System.Text.Json gives `[JsonIgnore]` (see the probe above). Today a member under `XmlIgnoreCondition.Always` gets no model at all (`TryCreateModel` returns `false`); it gets one, with no accessors, so that it is in `Properties`.

## Lifecycle

A type info has two states.

**Mutable.** What a resolver returns. It holds the facts of the type and of each member: names, kinds, accessors. Nothing is bound: no member type has been resolved, no UTF-8 name encoded, no constructor mapped.

**Read-only.** What the options hand out. It is bound, and every setter and every change to `Properties`, `ElementChoices` and `PolymorphismOptions.DerivedTypes` throws `InvalidOperationException`: "This XmlTypeInfo instance is marked read-only or has already been used in serialization or deserialization."

The steps, for `options.GetTypeInfo(type)`:

1. The options ask `TypeInfoResolver`, under the lock they resolve under today.
2. The resolver creates the mutable type info. For an object this is everything `XmlObjectBinder` does up to and including `CreateModels`: discovery, the attributes, the naming policies, `DefaultPropertyMapping`, the ignore conditions, member order. Each `XmlMemberModel` becomes an `XmlPropertyInfo`.
3. The resolver runs its modifiers on it, in order. `DefaultXmlTypeInfoResolver` runs `Modifiers`; a resolver from `WithAddedModifier` runs the inner resolver's first, then its own. A resolver in a chain runs its modifiers only on the type info it created; `WithAddedModifier` over a chain applies to all of them.
4. The options store the type info in the cache, then make it read-only and bind it: everything `XmlObjectBinder.Bind(Type, models, …)` does today (extension data, simple content, the constructor map, the bound members, object creation handling, name collisions, sibling namespaces, polymorphism). The models are read back from `Properties`, so binding sees what the modifiers left.
5. If binding throws, the type info is removed from the cache and the exception reaches the caller, as today. The next request runs the resolver and the modifiers again.

What follows from this:

- **A cycle is safe.** A member of the type's own type asks the options during step 4 and gets the instance that is in the cache: read-only, binding in progress. That is what it gets today.
- **A modifier must not ask the options for a type that is still being created.** Between steps 1 and 4 the type is not in the cache, so asking for it would run the resolver and the same modifier again, without end; that is what System.Text.Json does. The options keep the set of types that are between those steps, and a request for one of them throws `InvalidOperationException`: "The type info for 'T' is being created, so it cannot be resolved from a modifier. A modifier changes the type info it is given and reads the others from XmlPropertyInfo." This is the type itself, and any type whose own modifier asked for something that leads back to it. A modifier may resolve a type that is not in that set, but nothing here needs it to: what a modifier has to know about a member's type is on the `XmlPropertyInfo` (`PropertyType`, `IsScalar`, `CollectionMapping`, `IsMixed`).
- **A modifier runs once per type per options instance**, under the options' lock, so it needs no synchronization of its own for that options instance. It must not capture state that changes: the contract it leaves is cached.
- **A modifier sees every type**, not only objects. For a type whose `Kind` is not `Object`, `Properties` is empty, and a change to it, or setting `CreateObject`, `PolymorphismOptions` or a callback, throws `InvalidOperationException`: "Invalid XmlTypeInfo operation for XmlTypeInfoKind 'None'." (with the kind it has). `CreateProperty` succeeds for any kind, as `CreateJsonPropertyInfo` does in System.Text.Json; the property has nowhere to go. `RootName` can be set on every kind. One departure: System.Text.Json accepts `CreateObject` for `Enumerable` and `Dictionary`. Here a collection is built through its collection binding, which no attribute or option replaces, so the rule above keeps it out.
- **A converter of the caller's wins, as today.** A type with a converter in `Converters` or `[XmlConverter<T>]` has `Kind` `None`. A modifier cannot turn it into an object.
- **An exception from a modifier is not wrapped.**
- **`resolver.GetTypeInfo(type, options)` called directly** returns a new mutable instance each time, modifiers applied, and does not touch the cache of `options`. Today the object type info stores itself in the options from its constructor (`XmlObjectTypeInfo<T>`); that moves to step 4.
- **A type info passed to `XmlSerializer`** (the `XmlTypeInfo<T>` overloads) is made read-only and bound on first use if it is not already. The check is one field read on a path that is otherwise unchanged.
- **`XmlTypeInfo.MakeReadOnly()`** locks without binding. Binding still happens on first use.
- **`options.GetConverter(type)`** returns `GetTypeInfo(type).Converter` for a type whose `Kind` is `Object`, so a converter a caller holds writes the modified contract. Today it returns the converter from the options' converter cache, and an `XmlObjectConverter<T>` creates its one type info in its constructor and returns it from `CreateTypeInfo` for good. That pairing is what task 1 breaks: the resolver creates a converter and a type info together for each call, and the converter cache holds the pair the options bound.
- **Options become read-only** when they first resolve, as today. The modifier list of the resolver freezes at the same moment: `Modifiers.Add`, `Insert`, `Remove`, `Clear` and the indexer setter then throw `InvalidOperationException`, "Cannot add callbacks to the 'Modifiers' property after the resolver has been used for the first time."
- **`DefaultXmlTypeInfoResolver.Instance` has a frozen, empty list from the start.** It is the resolver of `Default`, `Web`, `Strict`, `Bcl` and of every reflection overload, so a modifier on it would change every caller in the process. `Add` throws `InvalidOperationException`: "DefaultXmlTypeInfoResolver.Instance is shared and cannot be given modifiers. Create a DefaultXmlTypeInfoResolver."

### What is checked when the type info is made read-only

Every refusal the binder has today is made here, against `Properties` as the modifiers left it, with the message it has today: a member that is both an attribute and text, a complex type as an attribute or as text, two extension-data members or one of the wrong type, two members with one XML name ("unrelated members with one XML name are an error", K53), a required member that is never read, text beside child elements, a choice that names a type the member cannot hold.

Added, because only a modifier can cause them. Each is an `InvalidOperationException` with a message that names the type and the member, and each has a test with the exact message:

- An `XmlPropertyInfo` that was created by another type info, or that is in `Properties` twice.
- `CollectionMapping` or `ItemName` on a member that is not a collection.
- A non-empty `ElementChoices` on a member that is mapped as an attribute or as text.
- A `CustomConverter` whose `CanConvert(PropertyType)` is `false`.

Three states only a modifier can produce are **not** errors:

- A property whose `Get` and `Set` are both `null` and that fills no constructor parameter is left out, as System.Text.Json leaves it.
- A property removed from `Properties` that fills a constructor parameter: the parameter is then one that belongs to an ignored member and takes its default (`XmlObjectBinding`, the `constructorMap[i] < 0` case).
- An attribute and an element with one local name. They are two XML names, and are allowed today.

## Accessors are typed

`Get`, `Set` and `ShouldSerialize` are on `XmlPropertyInfo<TDeclaring, TValue>`. They are typed by the type that declares the member and by the member's type, which is how the engine holds them (`XmlBoundProperty<TDeclaring, TValue>`), so nothing is wrapped and no value is boxed. System.Text.Json's are `Func<object, object?>` and box.

- A property whose accessors were **not assigned** is bound from the delegates the resolver gave it and its ignore condition, exactly as today.
- An accessor that **was assigned** replaces the engine's delegate. Either way the engine makes one call.
- The untyped `XmlPropertyInfo` has no accessors. A modifier that runs for every type and has to change one tests the property for the typed class (`if (property is XmlPropertyInfo<Order, decimal> total)`). The base exists because `Properties` holds members of different types ([typed first](28-typed-first.md), reason 1).
- A contract no modifier touches costs what it costs today. `XmlSerializerAllocationTests` holds this: the bytes allocated for a write and a read through a resolver with a modifier that only reads the contract equal those with no modifier.

## Properties a modifier adds

`XmlTypeInfo<T>.CreateProperty<TValue>(string name)` returns an `XmlPropertyInfo<T, TValue>` that is not yet in `Properties`, with `Name` set to `name` in no namespace, mapped as an element, with `Get` and `Set` `null`. The modifier sets what it needs and adds it.

- The member is closed over `T` and `TValue` at compile time, so the method has no trimming or Native AOT requirement.
- There is no form that takes a `Type`. System.Text.Json has only that form (`CreateJsonPropertyInfo(Type, string)`), which needs dynamic code. A modifier that adds a property writes its accessors, so it knows the type.
- The type info of `TValue` is resolved from the options when the type is bound, like any member's. Under a generated context the type must be one the context lists or reaches.

This is how one member is read from two names. The second property has only a `Set`, so it is read and never written:

```csharp
static void ReadDirectoryFromEither(XmlTypeInfo<TabConfig> tab)
{
    // <Tab Directory="x"/> is the form that is written. <Tab><Directory>x</Directory></Tab> is read too.
    tab.GetProperty<string?>(nameof(TabConfig.Directory))!.IsAttribute = true;

    var element = tab.CreateProperty<string?>(nameof(TabConfig.Directory));
    element.Set = static (ref TabConfig config, string? value) => config.Directory ??= value;
    tab.Properties.Add(element);
}

// The modifier is registered for the one type it is written for.
resolver.AddModifier<TabConfig>(ReadDirectoryFromEither);
```

And this is `DefaultPropertyMapping.Attribute` for some types and not others. For a type that has no mapping attributes, it leaves exactly what the option leaves: the scalar members are attributes and the rest are elements.

```csharp
static void IntegrationsAreAttributes(XmlTypeInfo typeInfo)
{
    if (typeInfo.Kind is not XmlTypeInfoKind.Object || typeInfo.Type.Namespace != "Mintlify.Core.Models.Integrations")
    {
        return;
    }

    foreach (var property in typeInfo.Properties)
    {
        if (property.IsScalar)
        {
            property.IsAttribute = true;
        }
    }
}
```

The option leaves a member alone when it carries `[XmlElement]`; a modifier cannot see that from the contract, and reads it from `AttributeProvider` if it matters. `IsAttribute = true` on a member whose type is not a single value is refused when the type is bound, with the message `[XmlAttribute]` gets.

## `AttributeProvider`

A modifier that acts on the caller's own attributes reads them from `XmlPropertyInfo.AttributeProvider`.

- The reflection resolver sets it to the `PropertyInfo` or `FieldInfo`.
- A generated context sets it from `XmlPropertyInfoValues<TDeclaring, TValue>.AttributeProviderFactory`, which the generator emits as `static () => typeof(T).GetProperty("Name", …)` with the type and the name as literals, so the trimmer keeps the member. It is called when `AttributeProvider` is first read. Confirm the emitted form against System.Text.Json's generator output before writing it; this design has not probed that.
- The `Attributes` array the generator emits today is not this: it holds only XmlEssentials attributes, constructed, for the binder.

## Source generation and the write fast path

A generated context needs no change to accept modifiers: `WithAddedModifier` wraps it, and the wrapper asks it through `IXmlTypeInfoResolver.GetTypeInfo(type, options)`, which already creates type info for options that are not the context's own.

Three things change.

1. **No fast path under a modifier.** A `SerializeHandler` was compiled from the attributes and writes the members itself, and it writes the members of the types in its `directTypes` inline, so a modifier on type B silently changes nothing in a document whose root is type A. `XmlMetadataServices.CanUseSerializeHandler` returns `false` when the resolver of the options, or any resolver in its chain, carries a modifier. The engine then writes from the metadata, which is what `Default` mode promises. A type generated in `Serialization` mode has no metadata, so under a modifier it throws the `InvalidOperationException` it throws today for options it was not compiled for.
2. **A changed contract drops its own handler.** A type info records that a setter or a list change was made on it or on one of its properties. When it is bound, a type info that was changed discards its `SerializeHandler`. This covers a caller who takes a type info from a resolver, changes it by hand and passes it to `XmlSerializer`, with no modifier anywhere.
3. **The typed properties of a context go through the cache.** The generator emits `public XmlTypeInfo<Person> Person => _Person ??= __Create_Person(Options)`, which today stores the type info in the options as a side effect. It becomes `(XmlTypeInfo<Person>)Options.GetTypeInfo(typeof(Person))`, so the property returns the read-only instance the options use. `GetTypeInfo(Type)` and the interface method create a new mutable instance on each call; they are what the options call, and calling the property from them would recurse.

Interceptors (K42) are unaffected: a call that passes options is not intercepted, and a call that passes none uses the context's own options, which have no modifier unless the context was constructed over options whose resolver the caller then replaced.

## Trimming and Native AOT

- `DefaultXmlTypeInfoResolver`, its constructor and `Modifiers` carry the requirements the class carries today.
- `XmlTypeInfoResolver.WithAddedModifier` and `Combine`, every member of `XmlTypeInfo`, `XmlTypeInfo<T>`, `XmlPropertyInfo` and `XmlPropertyInfo<TDeclaring, TValue>`, `XmlPolymorphismOptions`, `XmlDerivedType`, `XmlElementChoice` and `XmlParameterInfo` carry **no** requirement and produce no warning. No warning is suppressed anywhere.
- A derived type or an element choice a modifier adds names a type the context must list or reach. A type it does not is "not supported", as it is for an attribute today.
- `CustomConverter` on a member whose type is `T?` for a struct `T`: the reflection resolver wraps a converter for `T`, as it does for `[XmlConverter<T>]`. Generated metadata has the wrapper only for a member that had the attribute when it was compiled (`CreateNullablePropertyInfo`), so over a context the converter must convert `T?` itself; one that does not is the `CanConvert` refusal above.
- Proved before the rest depends on it (task 3), as [single engine](25-single-engine-metadata.md) did: `Tests.Aot` publishes with no IL warning and runs a rename, a move to an attribute, an added property through `CreateProperty<TValue>`, a typed `Set` on a member of a struct, a `CustomConverter`, a replaced `CreateObject`, a derived type added through `PolymorphismOptions`, and a callback, all through `AppXmlContext.Default.WithAddedModifier(…)` with the reflection switch off.

## Callbacks

`OnSerializing`, `OnSerialized`, `OnDeserializing` and `OnDeserialized` are on `XmlTypeInfo<T>` and are called with the object, typed: before its members are written, after the last is written, after it is created and before a member is set, and after the last member is set. For a type created through a constructor with parameters, `OnDeserializing` is called after the constructor returns, which is the first moment there is an object.

They are the one thing here the engine does not already do. Each is a `null` check per object on the read or the write path of `XmlObjectConverter<T>`. A type with a callback is not resumable in the middle (`XmlObjectBinding`, the resumable check) unless the callbacks are carried through the resume; decide that in the task, with a test either way. The callbacks are task 8 and can be cut without touching the rest.

## Tests

The rules in [testing](14-testing.md) apply: every assertion is on the whole document or the whole exception message.

- **`XmlSerializerModifierTests`.** One test per row of the counterpart table. Each writes and reads a type through a modifier and requires the document, and the value read back, to equal what the same type gives with the counterpart attribute. Two POCOs per row: one with the attribute, one bare with a modifier.
- **Parity.** Every case runs through `new DefaultXmlTypeInfoResolver { Modifiers = { m } }` and through a generated context `.WithAddedModifier(m)`, and requires one result, in the style of `XmlSerializerGeneratedParityTests`.
- **Lifecycle.** Order of modifiers; inner before outer; once per type per options; a chain; read-only after resolve with the exact message; the frozen list with the exact message; `Instance.Modifiers` with the exact message; a `null` modifier; a throwing modifier leaves nothing cached and throws again; a direct resolver call returns a new mutable instance; a self-referencing type; a modifier that asks for the type it is modifying, and one that asks for a type that leads back to it, each with the exact message.
- **Fast path.** A context in `Default` mode with a modifier that renames a member of a nested type writes the new name; the same context with no modifier still uses the handler (an internals test on the type info, not a timing).
- **Round trip.** A modified contract holds under `XmlRoundTripTests`: what it writes it reads back exactly.
- **Both XML versions** are not affected: no reader or writer path changes.
- **Allocation.** The test in [Accessors keep their types](#accessors-keep-their-types).
- **AOT.** The list under [Trimming and Native AOT](#trimming-and-native-aot).
- **Coverage gate** at 100% line and branch before any task is called done.

## Tasks

| # | Task | Status |
| --- | --- | --- |
| 0 | **`PropertyType` is a `Type`.** `XmlPropertyInfo.PropertyType` holds the declared type of the member, as `JsonPropertyInfo.PropertyType` does: `int?` stays `Nullable<int>`, a collection member has the collection's type, and a member written through a converter or as a typed element keeps its declared type. Sample 33, its guide, the two conceptual pages and [serializer API](07-api-serializer.md) follow. Test: `XmlSerializerMetadataTests.PropertyType_IsTheDeclaredTypeOfTheMember`. | **Done** 2026-11-26 |
| 0a | **Settle the breaking changes** under [Breaking changes to decide](#breaking-changes-to-decide), then rewrite the [public API](#public-api) block typed-first. | **Done** 2026-11-26 |
| 0b | **Move the library's own callers to the generic path.** Find every call inside `/src` to an `XmlSerializer` overload that takes a `Type`, an untyped `XmlTypeInfo` or a context, and replace it with the generic one where the type is known. Record which overloads keep a caller and why. No public change. | Not started |
| 1 | **Two-phase type info, no behaviour change.** Split object type info into create (facts and models) and bind. Move the cache write from the `XmlObjectTypeInfo<T>` constructor to the options. Break the one-to-one pairing of `XmlObjectConverter<T>` and its type info (`_typeInfo`, `CreateTypeInfo`) and of the converter cache with it. `XmlPropertyInfo` is built from the model and the binder reads it back. First-use binding for a type info passed to `XmlSerializer`. The set of types being created, and its exception. Check: the whole suite and the gate, unchanged. | Not started |
| 2 | **Mutable contract.** The setters of `XmlTypeInfo`, `XmlTypeInfo<T>`, `XmlPropertyInfo` and the typed `XmlPropertyInfo<TDeclaring, TValue>` (which the reflection resolver closes over the declaring type, as generated metadata does), `GetProperty`, `Kind`, `IsReadOnly`, `MakeReadOnly`, the read-only checks, `XmlPolymorphismOptions`, `XmlDerivedType`, public `XmlElementChoice`, `XmlParameterInfo`, `PropertyType` as a `Type`. Typed accessors kept when not assigned. The checks under [What is checked](#what-is-checked-when-the-type-info-is-made-read-only). | Not started |
| 3 | **Prove it under Native AOT** with a hand-changed type info from a generated context in `Tests.Aot`, before the resolvers depend on it. `CreateProperty<TValue>` and `CustomConverter` are the two that may not survive; if either cannot be made warning-free, stop and say so. | Not started |
| 4 | **Resolvers.** Public constructor and `Modifiers` on `DefaultXmlTypeInfoResolver`; the frozen list; `XmlTypeInfoResolver.WithAddedModifier` and `Combine`; `OriginatingResolver`. | Not started |
| 5 | **`RootName` everywhere `[XmlRoot]` reaches.** `XmlTypeNames.ElementName` and `SequenceElementName` read the type's attribute today and know no options cache. An item named for its type and a sequence must take a modified name, which means the item type is resolved before its parent's member is named. | Not started |
| 6 | **Fast path.** `CanUseSerializeHandler` under a modifier; a changed contract drops its handler; generated typed properties go through the options. | Not started |
| 7 | **`AttributeProvider`.** Reflection sets it; `AttributeProviderFactory` on `XmlPropertyInfoValues<TDeclaring, TValue>`; the generator emits it. | Not started |
| 8 | **Callbacks.** The four, in `XmlObjectConverter<T>`, with the resumable-read decision. | Not started |
| 9 | **Tests** as listed, gate, full suite, AOT publish. | Not started |
| 10 | **Spec and docs.** [Serializer API](07-api-serializer.md) (the type info block, the shared-behaviour rows for the resolver and read-only), [object mapping](09-object-mapping.md), [source generation](10-source-generation.md), [AOT](12-aot-and-trimming.md), a key decision in [identity and decisions](01-identity-and-decisions.md), [PR plan](22-pr-plan.md). A guide, "Customize a contract", beside `custom-converters.mdx`, and the XML doc comments of every new member. | Not started |
| 11 | **Benchmarks.** The object rows with no modifier must not move; one row with a modifier. Run in the final benchmark pass, which the owner scheduled for the end. | Not started |

Tasks 1 and 2 are the risk. Task 1 changes when an object type is bound and who caches it, which every serializer path passes through; it lands alone, with no public change, so a regression is attributable.

## Open points

- **`PropertyType` changed type** (task 0, done). It was an `XmlTypeInfo` from the first draft of [serializer API](07-api-serializer.md) with no recorded reason, and nothing in the engine read it. A mutable contract cannot hold the resolved type info of a member, because resolving it is what binding does. The owner said on 2026-11-25 that the library shipped that day and a dependency on the old shape is very unlikely.
- **The callbacks** are new engine behaviour, not only new surface. Cut task 8 if they are not wanted yet.
- **`Combine`** is here because the class that holds `WithAddedModifier` is where System.Text.Json has it, and `TypeInfoResolverChain` already does the work. It is not needed by modifiers.
- **Owed, and now in the API block:** `XmlTypeInfo.Create<T>(options)` (a blank contract for a hand-written resolver), `ElementType` and `KeyType`, `ConstructorAttributeProvider`, per-type and per-member `NumberHandling`, and per-type `UnmappedMemberHandling`. An earlier draft left them out as work a modifier does not need; [Direction](#direction), decision 1, puts them back. Still owed and not designed: `CreateObject` for collections, which the lifecycle section refuses, and the two attributes (`[XmlNumberHandling]`, `[XmlUnmappedMemberHandling]`) with their engine support. A type-level attribute for `DefaultPropertyMapping` has no System.Text.Json counterpart and stays out unless the owner asks for it.

## Breaking changes to decide

Shipped members whose shape would have to change, as opposed to members that are only added. Found two ways: a comparison of signatures with the System.Text.Json reference source for every type and member name the two libraries share (`stj-signature-audit.cs` and `stj-signature-audit.txt` in the repository root; it compares property types, settability, modifiers and method signatures), and a reading of the contract types against [Direction](#direction), decision 2. Nothing in this table is decided or done.

| # | Member | Today | Proposed | Why now | What breaks |
| --- | --- | --- | --- | --- | --- |
| B1 | `XmlTypeInfo.CreateObject` | **Done 2026-11-26**, read-only; the setter comes with the mutable contract (task 2). Was: `Func<object>?` on the untyped base | `Func<T>?` on `XmlTypeInfo<T>` only, settable | Modifiers make it settable. Made settable where it is, the untyped member is fixed for good, and a typed one beside it is the duplication System.Text.Json has. | Code that reads `CreateObject` from an untyped `XmlTypeInfo`. It would cast to `XmlTypeInfo<T>` first. |
| B2 | `XmlPropertyInfoValues<TValue>.Getter`, `Setter`, `DeclaringType` | **Done 2026-11-26** as designed below. Was: `Func<object, TValue>`, `Action<object, TValue>`, and the declaring type as a `Type` property | `XmlPropertyInfoValues<TDeclaring, TValue>` with a typed getter and a setter that takes the object by reference | It is the surface generated code is compiled against. The object is untyped today so that a setter can change a boxed struct; a by-reference setter does that with no box. Not proved: it changes `XmlBoundProperty` and every generated context, so it needs a trial first. | A library compiled with a context generated by 1.0 and run against the new runtime. Source that regenerates is unaffected. Hand-written metadata changes its type arguments. |
| B3 | `XmlPropertyInfo.Order` and the order rule | **Done 2026-11-26.** Was: unset is `-1`, members with an order come first, then the rest in declaration order | `Order` is an `int?`, `null` when the member sets none. Members sort by order; `null` is placed as `0`, so a negative order goes before the unordered members and a positive one after, as System.Text.Json has it. `[XmlElement]` and `[XmlArray]` record whether `Order` was set, so `-1` and `0` are orders like any other ([K36](01-identity-and-decisions.md#key-decisions)) | The owner's rule: any number a member sets is intent, and treating `null` as zero gives the same placement while keeping the intent apart. | The element order of any type that mixes ordered and unordered members. A type that orders every member with values of zero or more is unaffected. |

### B2: the design

Written before the work, so it can be picked up from here.

```csharp
public delegate void XmlPropertySetter<TDeclaring, TValue>(ref TDeclaring instance, TValue value);

public sealed class XmlPropertyInfoValues<TDeclaring, TValue> : XmlPropertyInfoValues
{
    public Func<TDeclaring, TValue>? Getter { get; init; }
    public XmlPropertySetter<TDeclaring, TValue>? Setter { get; init; }
    // Attributes, IsGetNullable, IsProperty, IsPublic, IsRequired, IsSetNullable, IsVirtual, PropertyName as before.
    // DeclaringType is gone: it is typeof(TDeclaring).
}

public static class XmlMetadataServices
{
    // Each helper that takes the values class gains TDeclaring.
    public static XmlPropertyInfoValues<TDeclaring, TValue> CreateStructPropertyInfo<TDeclaring, TValue>(XmlPropertyInfoValues<TDeclaring, TValue> propertyInfo)
        where TDeclaring : struct;   // new: a member declared by a value type
}
```

- **`XmlPropertyInfoValues<TValue>` is removed**, not kept beside the new class.
- **One setter delegate**, `XmlPropertySetter<TDeclaring, TValue>`, takes the object by reference so that it can set a member of a value type. The modifier API uses the same delegate for `XmlPropertyInfo<TDeclaring, TValue>.Set`; an `Action<TDeclaring, TValue>` cannot set a struct member.
- **`TDeclaring` is the type that declares the member**, which may be a base class of the type being read. The hiding rule, the per-class field order and the messages read it.
- **The engine holds the typed delegates directly.** `XmlBoundProperty<TDeclaring, TValue>` was already generic and was closed over `object`; it is now closed over the declaring type, and casts the instance where the generated lambda cast it before. No typed delegate is wrapped in an untyped one, extension data included.
- **A value type under construction stays boxed**, as today. Reaching into the box needs `Unsafe.Unbox<T>`, which is constrained to value types, so code that is generic over any declaring type cannot call it. `CreateStructPropertyInfo`, constrained to value types, supplies it, as `CreateValuePropertyInfo` supplies the held box. A setter on a value type with no such call is refused when the type is bound.
- **Measured on 2026-11-26** ([optimization](23-optimization.md#after-the-typed-first-changes-2026-11-26-net110-shortrun-hyper-v)): a member of a value type is set through two delegate calls (the unbox, then the setter) where it was one, and its getter receives the struct by value; side by side with the committed build, 1,000 value-type objects through generated metadata read and write in the same time and bytes. On the reflection path a field of a value type boxed the struct on every get and set, which was a regression; the field accessors are now emitted and box nothing.
- **Stop** if the value-type path needs an object-layout trick or a suppressed IL warning.

Compared and left alone, with the reason:

- **`XmlPropertyInfo.Name` is an `XmlName`**, where System.Text.Json has a `string`. An XML name carries a namespace.
- **`XmlPropertyInfo` is sealed, `XmlTypeInfo<T>` is abstract, `DefaultXmlTypeInfoResolver` and `XmlException` are sealed.** System.Text.Json differs on each. None has a public constructor a caller could have used, so changing them later breaks nothing.
- **`XmlConverterAttribute` and `XmlDerivedTypeAttribute` are abstract with generic forms.** Deliberate: an attribute that names a type takes it as a type argument.
- **`XmlObjectInfoValues<T>.ObjectWithParameterizedConstructorCreator` takes `object?[]`.** The arguments of a constructor have different types.
- **`XmlParameterInfoValues.DefaultValue` is an `object`.** It describes a parameter and nothing calls through it; a typed form would add a type argument for no gain.
- **`XmlMetadataServices.CreateValueInfo<T>` takes an `XmlConverter<T>`**, where System.Text.Json takes an untyped converter. Already the typed shape.
- **The `XmlSerializer` overloads that take a `Type`, an untyped type info or a context** stay ([Direction](#direction), decision 3). System.Text.Json's untyped `(object, JsonTypeInfo)` overloads, which this library does not have, are not added.
- **Setters that System.Text.Json has and this library lacks** (`Name`, `Order`, `IsRequired`, `CreateObject`) are additions.

## Reference: what System.Text.Json's contract surface is for

A reading of the System.Text.Json surface that XmlEssentials has no counterpart for, sorted by whether a developer needs it or whether it is there because of how the library grew. It is reference for deciding scope, not a decision.

The list of gaps comes from the reference source in `dotnet/dotnet` (`src/runtime/src/libraries/System.Text.Json/ref/System.Text.Json.cs`, commit `870dac7`), compared by type and member name with `src/CloudNimble.XmlEssentials`. The raw output is `stj-surface-audit.txt` in the repository root, and `stj-surface-audit.cs` produces it. The source confirms that `AddContext<T>()` and `IgnoreNullValues` are obsolete. The release each member arrived in is from memory and was not checked.

`JsonTypeInfo` and `JsonTypeInfo<T>` are both needed. A resolver is asked for a type at run time (`GetTypeInfo(Type)`) and a modifier is an `Action<JsonTypeInfo>` that runs for every type, so neither can be written against `JsonTypeInfo<T>`. The generic class carries the typed parts (`Func<T>` for `CreateObject`, `Action<Utf8JsonWriter, T>` for `SerializeHandler`, the typed `Serialize<T>` entry points) with no boxing. `JsonConverter` and `JsonConverter<T>` split for the same reason.

| Item | Verdict | Why |
| --- | --- | --- |
| `JsonTypeInfo` + `JsonTypeInfo<T>` | Needed | Untyped handle for resolvers and modifiers, typed one for delegates without boxing |
| `Kind`, `Properties`, `CreateJsonPropertyInfo`, `IsReadOnly`, `MakeReadOnly`, `OriginatingResolver` | Needed | This is the modifier feature itself |
| `JsonPropertyInfo.Get`, `Set`, `ShouldSerialize`, `CustomConverter`, `AttributeProvider`, `IsExtensionData`, `IsRequired`, `Order`, `Name` | Needed | What a modifier actually edits |
| `Modifiers`, `WithAddedModifier`, `Combine`, public resolver constructor | Needed | Entry points to the feature |
| `PolymorphismOptions`, `JsonDerivedType` | Needed | Only way to set derived types without attributes |
| `NumberHandling`, `UnmappedMemberHandling`, `ObjectCreationHandling`, `PreferredPropertyObjectCreationHandling` (type and member level, plus the attributes) | Needed | Each is an option that can be overridden per type or member |
| `IsGetNullable`, `IsSetNullable`, `AssociatedParameter`, `JsonParameterInfo`, `ConstructorAttributeProvider` | Needed if nullable and constructor-parameter enforcement should be visible to modifiers | Later additions, but they expose real behavior |
| `ElementType`, `KeyType` | Needed, minor | Read-only facts for collection contracts |
| Non-generic `JsonPropertyInfo` with `Func<object, object?>` accessors | Bolt-on | It shipped as an opaque type in .NET 6 and was opened in .NET 7, so the public accessors box. Here they are typed ([Accessors are typed](#accessors-are-typed)). |
| `JsonTypeInfo.CreateObject` as `Func<object>`, shadowed by `Func<T>` on the generic | Bolt-on | Same history; two properties for one thing |
| `IJsonOnSerializing` and the other three interfaces, alongside the four `On…` delegates | One of the two is redundant | Interfaces came in .NET 6, delegates in .NET 7. The interfaces are what people use; the delegates are how a modifier reaches the same hook. |
| `JsonPropertyInfoValues<T>`, `JsonObjectInfoValues<T>`, `JsonCollectionInfoValues<T>`, `JsonParameterInfoValues`, `JsonMetadataServices.Create…Info` | Bolt-on | A second way to build a contract, made for the .NET 6 generator when `JsonTypeInfo` was not settable. Only generated code calls it. |
| `JsonMetadataServices` per-type converter properties (about 40) | Plumbing | Public only because generated code lives in the user's assembly |
| `JsonSerializerContext.GeneratedSerializerOptions` | Plumbing | Same reason |
| `JsonSourceGenerationOptionsAttribute` mirroring every option | Bolt-on, but used | Exists because the fast path bakes options in at compile time. Every new option has to be added twice. |
| `JsonKnownReferenceHandler` | Bolt-on | An enum because an attribute cannot take a `ReferenceHandler` object |
| `UseStringEnumConverter`, `JsonStringEnumConverter<T>`, `JsonNumberEnumConverter<T>` | Bolt-on | Fixes for the non-generic `JsonStringEnumConverter` needing run-time code generation under AOT |
| `AddContext<T>()`, `IgnoreNullValues` | Dead | Both marked obsolete in the source |
| `JsonException` serialization constructor, `GetObjectData` | Dead | Obsolete binary-formatter support |
| `JsonConverter.Type`, `ReadAsPropertyName`, `WriteAsPropertyName` | Needed only for dictionary keys | Whether XML needs them depends on how dictionary keys are written |
| `JsonSchemaExporter` (3 types) | Separate feature | JSON Schema. The XML analogue would be XSD export, which is a new product decision. |
| Unions and type classifiers (about 10 types) | Separate feature | New in .NET 11 |
| DOM, reader, writer and serializer overload gaps | Needed, unrelated to contracts | Ordinary missing surface, not architecture |

The "Needed" rows are owed in full ([Direction](#direction), decision 1). The "Bolt-on", "Plumbing" and "Dead" rows are where this library takes a better shape or builds nothing.

## Progress notes

Add a dated line here whenever a task finishes or a design point changes, so the work can be picked up from this file alone.

- 2026-11-25: designed. System.Text.Json probed on .NET 11.0.0. Read for the design: `XmlTypeInfo`, `XmlObjectTypeInfo<T>`, `XmlPropertyInfo`, `DefaultXmlTypeInfoResolver`, `XmlSerializerContext`, `XmlObjectBinder` (`Bind`, `TryCreateModel`, `CreateProperty`), `XmlMemberFacts`, `XmlMemberModel`, `XmlObjectBinding`, `XmlBoundProperty`, `XmlObjectConverter<T>`, `XmlMetadataServices` (`CanUseSerializeHandler`, `CreateObjectInfo`), `XmlTypeNames`, the options' type info cache, and `ContextEmitter`. Not read: the collection and dictionary type infos, `XmlResumableReader` and `MapConstructor`; the statements about `Kind` for collections and about resumable reads are requirements, not descriptions of code that was checked.
- 2026-11-25: a second probe corrected the draft. `[JsonIgnore]` keeps an `Always` property in `Properties` with no accessors, and `WhenWriting` is a `ShouldSerialize`, not a `null` `Get`. `CreateJsonPropertyInfo` works for every kind. A modifier that resolves its own type recurses without end in System.Text.Json; this design refuses it. `IsScalar` was added so that the per-type mapping example needs no resolution.
- 2026-11-26: the public surface of System.Text.Json was compared with XmlEssentials from the `dotnet/dotnet` source. The design leaves out members that an exact clone needs; see the reference table. The API block and the task list have not been corrected yet.
- 2026-11-26: the owner set the direction (capability clone, typed first, shipped overloads stay). Task 0 done: `PropertyType` is the declared `Type`; 6,092 tests pass and the gate reports 100% line and branch. A signature-level comparison with the System.Text.Json source and a reading of the contract types gave three breaking changes to decide (B1 to B3). The API block is pending those.
- 2026-11-26: B3 done at the owner's direction. `XmlPropertyInfo.Order` is an `int?`; a member that sets no order is placed as zero; the reflection resolver and the generator sort the same way. Tests that pinned "ordered members first" now state the new rule, and `Order_IsNullWhereAMemberSetsNone_AndZeroIsAnOrderAMemberSet` is new. 6,094 tests pass, gate at 100% line and branch. B1 and B2 were still open.
- 2026-11-26: B1 done. `CreateObject` is a `Func<T>?` on `XmlTypeInfo<T>` and the untyped base has none. It stays read-only until the mutable contract (task 2).
- 2026-11-26: B2 done as designed under [B2: the design](#b2-the-design). `XmlPropertyInfoValues<TDeclaring, TValue>` replaces `XmlPropertyInfoValues<TValue>`; `XmlPropertySetter<TDeclaring, TValue>` is new and public; the four `XmlMetadataServices` helpers that take the values class gained `TDeclaring`; `CreateStructPropertyInfo` is new. `XmlBoundProperty` and the extension-data binding hold the typed delegates, through `XmlMemberAccess`. The reflection resolver no longer wraps a class property's getter in a second delegate. The generator emits typed lambdas, and a setter is `static (ref T obj, TValue value) => …`. No object-layout trick and no suppressed warning was needed. Tests: `ValueType_IsSetInPlace_ThroughEveryKindOfMember` (a field, a property, an init-only property and extension data of a struct, generated and reflected) and the guard in `XmlSerializerInternalsTests`. Not measured, for the final benchmark pass: a member of a value type is set through two delegate calls, and its getter takes the struct by value. With B1 to B3 settled, task 0a is down to rewriting the API block.
- 2026-11-26: the public API block is rewritten typed-first, with the sections that described untyped accessors and `CreateXmlPropertyInfo`. Accessors are on `XmlPropertyInfo<TDeclaring, TValue>`; the callbacks and `CreateObject` are on `XmlTypeInfo<T>`; a property is added with `CreateProperty<TValue>` and found with `GetProperty`; a derived type is made with `XmlDerivedType.Create<TBase, TDerived>`; a modifier is registered for one type with `AddModifier<T>`. The table under the block lists each departure from System.Text.Json's shape. The rule, the inventory of what is still untyped and the plan are [typed first](28-typed-first.md). The owner said on 2026-11-26 that listing a member with `[XmlIgnore]` in `Properties` is coming soon and is not a break to hold back.
- 2026-11-26: `XmlTypeInfo.Properties` returns a list of its own (`XmlPropertyInfoList`), not an array. An array let a caller replace an element through the indexer and threw `NotSupportedException` from `Add`. The list is read-only, as its type info is: every change throws `InvalidOperationException` with the read-only message this file gives, after the argument check, in the order System.Text.Json's list uses. The mutable half comes with task 2. Test: `TypeInfo_Properties_IsAListThatCannotBeChanged`. Found while checking and not changed: a member with `[XmlIgnore]` (`Always`) is left out of `Properties` (`XmlObjectBinder.TryCreateModel`), where System.Text.Json keeps it with no getter or setter. Listing it needs `Get` and `Set` on `XmlPropertyInfo` to say so, which is task 2; it will change `Properties.Count` for such types.

---

[Index](README.md) · [Previous: Expanded encoding support](26-expanded-encoding-support.md) · [Next: Typed first](28-typed-first.md)
