# Single engine: generated metadata

Part of the [XmlEssentials spec](README.md).

This file is the design and the record of making source generation work the way System.Text.Json's does: the generator emits **metadata**, and the one runtime engine reads and writes from it. It resolved D2 in [doc findings resolution](24-doc-findings-resolution.md) and is key decision K52. The work is done except the benchmark pass, which the owner scheduled for the end.

## The defect (as it was)

- `XmlSourceGenerationMode.Metadata` generates no metadata. For an object type the generator emits `DefaultXmlTypeInfoResolver.Instance.GetTypeInfo(typeof(T), options)`, the reflection resolver, so a metadata context is neither trim-safe nor Native AOT-safe.
- `Default` and `Serialization` emit the same code. `GenerationMode` on `[XmlSerializable<T>]` is read and discarded.
- The generated converters hand-write reading and writing, so the library has two engines. Everything the reflection engine does and the emitter does not is a refusal or a diagnostic: `ReferenceHandler`, populating an object, derived types (XMLESS008), member converters (XMLESS009), mixed content (XMLESS014), extension data, non-public members, and the naming policies and `DefaultPropertyMapping`, which are compiled in and refused when the options differ.

## What System.Text.Json does

Taken from the generator's real output and from a probe on .NET 10, not from memory.

- **Metadata** emits, per object type, a `JsonObjectInfoValues<T>` (an object creator, a creator that takes constructor arguments, the constructor parameters, a property initializer) and, per member, a `JsonPropertyInfoValues<T>` (declaring type, name, getter and setter delegates, the member's attribute facts). `JsonMetadataServices.CreateObjectInfo` and `CreatePropertyInfo` turn them into type info. The runtime engine does all reading and writing. Naming policies, ignore conditions, reference handling, polymorphism, populate and non-public `[JsonInclude]` members all work, because the engine applies the options when the type info is created.
- **Serialization** emits a write-only fast path. A `Serialization`-only context throws `InvalidOperationException` ("TypeInfoResolver 'X' did not provide property metadata for type 'T'.") on deserialize, and on serialize when the options are not the ones the fast path was compiled for.
- **Default** emits both. The fast path is used when the options allow it, and the metadata otherwise. Nothing is refused.
- The mode can be set per type on `[JsonSerializable]`.
- A member with an `init` accessor is passed to the object creator as a constructor argument, and its setter throws. A type with such a member is therefore "created through a constructor with parameters" and populates nothing, where the reflection resolver populates it.

## The design

One engine. `XmlObjectBinder` binds from **member facts**, not from `MemberInfo`. The reflection resolver fills the facts by reflection; a generated context fills them with code.

### Runtime

- `XmlMemberFacts` (internal, a plain class): the member's name, declaring type and member type; whether it is a field, has a public getter, has a setter, is an indexer, is `required`; what its nullable annotations refuse; its attributes as instances; a getter and a setter delegate; and a typed factory that creates the bound member. Every rule in the binder that called `member.GetCustomAttribute` reads the facts instead. No rule is duplicated.
- `XmlConstructorFacts` (internal): the parameters (name, type, default) and a delegate that creates the object from its arguments. Reflection wraps a `ConstructorInvoker`; generated code passes a lambda.
- Type-level attributes (`[XmlRoot]`, `[XmlPolymorphic]`, `[XmlDerivedType<T>]`, `[XmlObjectCreationHandling]`, `[XmlConverter<T>]` on a type) are still read from `typeof(T)`. Attributes on a type the program uses are kept by the trimmer; members and their attributes are not, which is why member facts are generated.
- Every run-time generic closure in the engine gets a statically closed route that generated code calls: the bound member (`MakeGenericMethod` in `Bind`), the collection binding (`XmlCollectionBinding.Create`), the held box for a value-type member of a constructed type (`UseHeldBox`), and the nullable wrapper round a member converter (`CreateMemberTypeInfo`).

### Public API (the names System.Text.Json uses)

```csharp
public sealed class XmlObjectInfoValues<T>
{
    public Func<T>? ObjectCreator { get; init; }
    public Func<object?[], T>? ObjectWithParameterizedConstructorCreator { get; init; }
    public Func<XmlParameterInfoValues[]>? ConstructorParameterMetadataInitializer { get; init; }
    public Func<XmlSerializerOptions, XmlPropertyInfoValues[]>? PropertyMetadataInitializer { get; init; }
    public Action<Utf8XmlWriter, T, XmlSerializerOptions>? SerializeHandler { get; init; }   // the write fast path
}

public abstract class XmlPropertyInfoValues { /* no public constructor */ }

public delegate void XmlPropertySetter<TDeclaring, TValue>(ref TDeclaring instance, TValue value);

public sealed class XmlPropertyInfoValues<TDeclaring, TValue> : XmlPropertyInfoValues   // TDeclaring is the type that declares the member
{
    public string PropertyName { get; init; }
    public bool IsProperty { get; init; }
    public bool IsPublic { get; init; }
    public bool IsRequired { get; init; }              // the required modifier
    public bool IsVirtual { get; init; }               // a virtual accessor; see "members of the same name" in object mapping
    public bool IsGetNullable { get; init; }
    public bool IsSetNullable { get; init; }
    public Func<TDeclaring, TValue>? Getter { get; init; }
    public XmlPropertySetter<TDeclaring, TValue>? Setter { get; init; }   // by reference, so it can set a member of a value type
    public Attribute[]? Attributes { get; init; }      // the member's XmlEssentials attributes, constructed
}

public sealed class XmlParameterInfoValues
{
    public string Name { get; init; }
    public Type ParameterType { get; init; }
    public int Position { get; init; }
    public bool HasDefaultValue { get; init; }
    public object? DefaultValue { get; init; }
    public bool IsNullable { get; init; }
}

public static class XmlMetadataServices
{
    public static XmlTypeInfo<T> CreateObjectInfo<T>(XmlSerializerOptions options, XmlObjectInfoValues<T> objectInfo);
    public static XmlPropertyInfoValues<TDeclaring, TCollection> CreateCollectionPropertyInfo<TDeclaring, TCollection, TElement>(XmlPropertyInfoValues<TDeclaring, TCollection> propertyInfo);
    public static XmlPropertyInfoValues<TDeclaring, TElement[]> CreateArrayPropertyInfo<TDeclaring, TElement>(XmlPropertyInfoValues<TDeclaring, TElement[]> propertyInfo);
    public static XmlPropertyInfoValues<TDeclaring, T?> CreateNullablePropertyInfo<TDeclaring, T>(XmlPropertyInfoValues<TDeclaring, T?> propertyInfo) where T : struct;
    public static XmlPropertyInfoValues<TDeclaring, TValue> CreateValuePropertyInfo<TDeclaring, TValue>(XmlPropertyInfoValues<TDeclaring, TValue> propertyInfo) where TValue : struct;
    public static XmlPropertyInfoValues<TDeclaring, TValue> CreateStructPropertyInfo<TDeclaring, TValue>(XmlPropertyInfoValues<TDeclaring, TValue> propertyInfo) where TDeclaring : struct;
    public static bool CanUseSerializeHandler(XmlSerializerOptions options, XmlNamingPolicy? elementNamingPolicy, XmlNamingPolicy? attributeNamingPolicy,
        XmlPropertyMapping defaultPropertyMapping, bool includeFields, params ReadOnlySpan<Type> directTypes);
}
```

`XmlTypeInfo` stays read-only. There is no `CreatePropertyInfo`: generated code creates the `XmlPropertyInfoValues<TDeclaring, TValue>` itself. `CreateCollectionPropertyInfo` names the item type of a collection member, `CreateArrayPropertyInfo` that of an array (the constructors of `System.Array` cannot be kept for Native AOT), `CreateNullablePropertyInfo` the underlying type of a nullable member that has its own converter, and `CreateValuePropertyInfo` a value-type member, whose value is held in a reusable box while a constructor's arguments are gathered; all four exist so that no generic type is closed at run time. `CanUseSerializeHandler` is the test generated code makes before it supplies a `SerializeHandler`. `IsGetNullable`, `IsSetNullable`, `IsProperty` and `IsPublic` default to `true`.

### What generated code may do that System.Text.Json's does not

- **`init`, `required` and non-public members are set through `[UnsafeAccessor]`**, which the library's target frameworks (.NET 10 and 11) have. The setter delegate of an `init` member calls the accessor, and an object with `required` members is created through a constructor accessor. A generated context then behaves as the reflection resolver does for these members, and a type with an `init` member is not "created through a constructor with parameters". This is a deliberate improvement on System.Text.Json, recorded so the parity tests are read correctly.
- **Member attributes are constructed, not looked up.** The generator turns each attribute on a member into `new XmlElementAttribute("n") { Order = 1 }` in the `Attributes` array. The engine's rules read them exactly as they read reflected ones.

### Modes

| Mode | Generated | Behaviour |
| --- | --- | --- |
| `Metadata` | Metadata for every listed and reached object type | The engine reads and writes. Every option and feature the reflection resolver has. AOT-safe. |
| `Serialization` | The write fast path only | Writes when the options are the ones it was compiled for. Otherwise, and on every read, `InvalidOperationException`: "The type info for 'T' was generated in XmlSourceGenerationMode.Serialization, which provides no property metadata. It can only write a type that has a write fast path, with the options it was compiled for. Use XmlSourceGenerationMode.Default or XmlSourceGenerationMode.Metadata." System.Text.Json's message names its resolver; ours names the mode and the way out. |
| `Default` | Both | The fast path writes when the options allow it; the engine does everything else. Nothing is refused. |

The mode on `[XmlSerializable<T>]` overrides the mode of the context for that type.

The generated **read** code is deleted: reading is the engine's, as in System.Text.Json. The generated rows are benchmarked in the final benchmark pass; the owner asked on 2026-11-23 that optimization and benchmarks wait for the end of the work.

## What this replaces

- Decision 31 in [doc findings resolution](24-doc-findings-resolution.md) ("a generated converter either follows an option or refuses it") and `XmlMetadataServices.VerifyGeneratedOptions`: under `Default` and `Metadata` nothing is refused.
- XMLESS008 (derived types), XMLESS009 (member converters), XMLESS014 (mixed content) and XMLESS016 (populate): the engine does all four. XMLESS017 stays as the engine's own error.
- The `NotSupportedException` for `ReferenceHandler` and for `Populate` on an object member.
- The naming policies, `DefaultPropertyMapping`, `IncludeFields` and the read-only switches being compiled in: they are applied when the type info is created.
- The open items in [open questions](20-open-questions.md) about what generated converters cannot do, D2, and the generated-converter part of D6.
- The guides: source-generation, source-generation-modes, reflection-vs-source-generation, native-aot.
- `XsdParser`, which builds `TypeSpec` directly and must emit the same metadata.

Every test that pins a refusal changes; list each one under "Tests that changed" in [doc findings resolution](24-doc-findings-resolution.md).

## Tasks

| # | Task | Status |
| --- | --- | --- |
| 1 | **Refactor, no behaviour change.** `XmlMemberFacts` and `XmlConstructorFacts`; the binder reads facts; reflection fills them. Check: the whole suite and the gate, unchanged. | **Done** |
| 2 | **Public metadata API.** The values classes and `CreateObjectInfo` / `CreatePropertyInfo` / `CreateCollectionPropertyInfo`; static routes for the held box, the collection binding, and the nullable member converter. Tests that build metadata by hand and require the same documents as the reflection resolver. | **Done**: 5135 tests pass, gate 100% line and branch |
| 3 | **Prove it under Native AOT** with hand-written metadata in `Tests.Aot`, before the generator depends on it: a constructor with a value-type parameter, a struct member, `int?` with a member converter, `List<int>` and `Dictionary<string, int>`, a derived type, `Preserve`, extension data, `Populate`. The reflection switch off. | **Done**: `Tests.Aot/ManualMetadata.cs` published with no IL warnings and ran every case. It was a spike, replaced in task 7 by `EngineFeatures.cs`, which runs the same cases through a generated context. |
| 4 | **Generator emits metadata.** `MetadataEmitter`: object info, property info with constructed attributes, `[UnsafeAccessor]` for `init`, `required` and non-public members, constructor facts. The walker lists derived types and member-converter types. `Metadata` mode uses it. | **Done**: `MetadataBuilder` |
| 5 | **Modes as System.Text.Json has them.** `Default` uses metadata plus the write fast path with fallback; `Serialization` is write-only and throws as specified; the per-type mode is honoured. Delete the generated read code and `VerifyGeneratedOptions`. | **Done** |
| 6 | **Diagnostics and tests.** Retire XMLESS008, 009, 014, 016. Rewrite the tests that pin refusals; the generated-versus-reflection parity tests must show one result everywhere. `XsdParser` emits metadata. | **Done**: the changed tests are listed in [doc findings resolution](24-doc-findings-resolution.md) |
| 7 | **AOT smoke through the generator**, coverage gate, full suite. | **Done**: 5772 tests pass, none skipped; gate 100% line and branch; `Tests.Aot/EngineFeatures.cs` publishes with no IL warnings and passes |
| 8 | **Benchmarks** for the generated rows, before and after; tables in [PR plan](22-pr-plan.md) and [optimization](23-optimization.md). | **Deferred by the owner** to the final optimization and benchmark pass. Not run. |
| 9 | **Spec and docs.** [Source generation](10-source-generation.md), [serializer API](07-api-serializer.md), [open questions](20-open-questions.md), [doc findings resolution](24-doc-findings-resolution.md) (D2, decision 31, changed tests), [key decisions](01-identity-and-decisions.md), the four guides, the reference notes. | **Done** |

## Progress notes

Add a dated line here whenever a phase finishes or a design point changes, so the work can be picked up from this file alone.

- 2026-11-23: design verified against System.Text.Json's generated output and a probe of its three modes. Run-time reflection in the engine inventoried: `Bind` (`MakeGenericMethod`), `XmlCollectionBinding.Create`, `UseHeldBox`, `CreateMemberTypeInfo`, `ConstructorInvoker`, struct getter and setter creation, `NullabilityInfoContext`, and 27 attribute lookups in `XmlObjectBinder`.
- 2026-11-23: tasks 1 and 2 done. What exists: `XmlMemberFacts`, `XmlConstructorFacts`, `XmlParameterFacts` (internal); `XmlObjectBinder.Bind<T>(options)` (reflection front), `Bind<T>(XmlObjectInfoValues<T>, options)` (metadata front), both ending in `Bind(Type, models, constructor, create, options)`; `CreateModels(facts, options)`; `XmlObjectConverter<T>(options, bind)`. Public: `XmlObjectInfoValues<T>`, `XmlPropertyInfoValues` / `XmlPropertyInfoValues<TValue>`, `XmlParameterInfoValues`, `XmlMetadataServices.CreateObjectInfo<T>`, `CreateCollectionPropertyInfo<TCollection, TElement>`, `CreateNullablePropertyInfo<T>`. There is no `CreatePropertyInfo`: generated code creates the `XmlPropertyInfoValues<TValue>` itself, and the class closes the bound member over `TValue`. Tests: `XmlSerializerMetadataTests` with the hand-written `MetadataResolver`, which runs every case through metadata and through reflection and requires one result. Known gap, by design for now: a member whose facts were generated never uses the reusable held box (`NoHeldBox`), so a value-type member of a constructed type is boxed once per object; a struct-constrained route can restore it.
- 2026-11-23: task 3 done. Under Native AOT only the type-info overloads of `XmlSerializer` are usable (the options overloads are `RequiresUnreferencedCode`), so the spike asks its resolver for `XmlTypeInfo<T>`.
- 2026-11-23: tasks 4 to 7 and 9 done. How it is built:
  - **Generator.** `MetadataBuilder.Build` writes each object type's metadata from symbols into `TypeSpec.Metadata` and `Accessors`; `BuildFromMembers` does the same for a class `XsdParser` generates. `ContextEmitter.EmitMetadata` emits `__Create_X(options)`, and `CreateExpression` calls it for every object type. `ConverterEmitter` emits only the write fast path, `__Serialize_X`. `TypeGraphWalker` still binds members, for the fast path and for diagnostics; `FastPathBlocked` marks a type that gets no fast path. `TypeSpec.Mode` carries the mode, per type (`ContextCollector`).
  - **Members emitted:** every instance property and field of the type and its base types, base types first, that is public or has `[XmlInclude]`; not indexers, not properties with no getter. The runtime applies every other rule. `[UnsafeAccessor]` is used for `init`, for an accessor or a member that is not accessible, and for a constructor that is not accessible or belongs to a type with `required` members.
  - **Runtime.** `XmlObjectBinder.Bind<T>(XmlObjectInfoValues<T>, options)` is the metadata front and `Bind<T>(options)` the reflection front; both end in the same `Bind`. `XmlObjectConverter<T>` calls the `SerializeHandler` when there is one, and `XmlObjectBinding.ThrowIfNoMetadata` throws for a `Serialization`-only type. `XmlMetadataServices.CreateObjectInfo` returns the caller's own converter for the type when the options or the type name one.
  - **Deleted:** the generated read code (`EmitRead`, `EmitPopulate` and their helpers), the `__ReadNil` and `ReadStringElement` context helpers, `VerifyGeneratedOptions`, `XmlMetadataServices.Read` / `ReadAttribute` / `AddItems` / `AddEntries` / `CreateCollection` / `RejectUnmappedAttribute` / `RejectUnmappedElement`, `XmlCollectionFactory<,>`, the populate, constructor-tail and root-name fields of `MemberSpec` and `TypeSpec` with the helpers that fed them, XMLESS008, 009, 014 and 016, and the `ManualMetadata.cs` spike.
- 2026-11-23: found while testing parity, and decided by the owner: members of the same name follow System.Text.Json (K53, [object mapping](09-object-mapping.md#members-of-the-same-name)). `XmlObjectBinder.CreateModels` applies the rules to the facts of either resolver; `XmlPropertyInfoValues<TValue>.IsVirtual` carries what it needs. This closed the edge noted earlier, that an override did not carry the attributes of the property it overrides: it carries none in either resolver.
- 2026-11-26: the member metadata is typed by the declaring type ([contract modifiers](27-modifiers.md), B2). `XmlPropertyInfoValues<TDeclaring, TValue>` has a `Func<TDeclaring, TValue>` getter and an `XmlPropertySetter<TDeclaring, TValue>` setter that takes the object by reference; `DeclaringType` is gone; `CreateStructPropertyInfo` marks a member that a value type declares. The API block above shows the new shapes.
- 2026-11-23: also found while testing parity and fixed: a member whose type or item type declares derived types was written by the fast path without its discriminator (the type now gets no fast path); `XmlMixedText` was not listed by a context with a mixed member; a non-public constructor marked `[XmlConstructor]` was refused.

---

[Index](README.md) · [Previous: Doc findings resolution](24-doc-findings-resolution.md) · [Next: Expanded encoding support](26-expanded-encoding-support.md)
