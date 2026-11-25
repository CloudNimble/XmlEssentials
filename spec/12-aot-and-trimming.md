# AOT and trimming

Part of the [XmlEssentials spec](README.md).

On `CloudNimble.XmlEssentials.csproj`:

```xml
<IsAotCompatible>true</IsAotCompatible>
<EnableTrimAnalyzer>true</EnableTrimAnalyzer>
```

- Reflection overloads: `[RequiresDynamicCode]`, `[RequiresUnreferencedCode]`. See "No suppression, and what carries a warning" below for every member that carries them.
- Context overloads: **no** AOT warning.
- Feature switch `IsReflectionEnabledByDefault` trims the reflection resolver.
- Test asset: `CloudNimble.XmlEssentials.Tests.Aot`, published with `PublishAot=true`, `InvariantGlobalization=true`, `IsReflectionEnabledByDefault=false`. Smoke-tests a source-generated `Person`, an XSD-generated `Order`, and a `Utf8XmlReader` walk. CI job `aot` publishes and runs it. This is [PR 8](22-pr-plan.md#pr-8-aot--trimming-tests).
- Sample: `CloudNimble.XmlEssentials.Samples.Cli` (`xmlessentials`) uses McMaster.Extensions.CommandLineUtils (builder API) and the same generated-context path. `PublishAot=true`.

Do not reference `System.Xml.Serialization` or `System.Xml.Schema` from the runtime library — that would drag AOT warnings back in. The assembly-ref test in [packaging](13-repo-and-packaging.md#forbidden-references-ci-test) is the gate (`System.Xml*` and `System.Private.Xml`).

XSD is compile-time only ([11](11-xsd-source-generation.md)). AOT publish must not embed `.xsd` files or resolve schemas. Generated types are registered on a context; `xs:any` is extension data, not `Type.GetType`. PR 8 smoke test includes an XSD-generated type with `IsReflectionEnabledByDefault=false`.

Feature switch XML: [XmlSerializer](07-api-serializer.md#xmlserializer).

## No suppression, and what carries a warning

The runtime library has **no** `UnconditionalSuppressMessage`, and the generators emit none and no `#pragma` for an `IL` warning (2026-11-25, the owner's decision; decision 85 in [doc findings resolution](24-doc-findings-resolution.md)). A suppression says the tool is wrong, and nothing checks that it stays true: with 56 of them, a clean Native AOT publish proved nothing.

- **Says it needs reflection** (`[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`): `DefaultXmlTypeInfoResolver` (the class), every `XmlSerializer` method that takes no `XmlTypeInfo` and no `XmlSerializerContext`, `XmlSerializerOptions.GetConverter`, `MakeReadOnly(bool)`, and the getters of `Default`, `Web`, `Strict` and `Bcl`. Inside the library: the converter factories for objects and collections, `XmlBuiltInConverters.GetReflected`, and the binder methods that read members, constructors and accessors or close a generic type.
- **Carries no warning, so it may not need one**: the overloads that take type info or a context, `XmlSerializerOptions.TypeInfoResolver`, `GetTypeInfo`, `MakeReadOnly()`, `XmlSerializerContext`, and all of `XmlMetadataServices`. Options with no resolver are given the reflection resolver only by a member of the first group; `GetTypeInfo` on options that have none throws.
- **One binder, facts from two sources.** `XmlObjectBinder.CreateProperty` binds a member from its facts alone. The reflection resolver completes the facts in `CreateReflectedProperty` (accessors, the wrapper of a nullable value, the box of a value type, the collection mapping); a generated context writes them. A fact that is missing is an `InvalidOperationException`, not a fallback to reflection.
- **What a type holds is named, not discovered.** Whether a type is a collection or a dictionary, and of what, is in a registry. A generated context fills it in a static constructor (`XmlMetadataServices.RegisterCollection`, `RegisterDictionary`), and names the constructed generic derived types of a closed type (`RegisterClosedDerivedType`). Creating `DefaultXmlTypeInfoResolver` installs the two pieces of code that look at interfaces and close a generic type, so a program that never creates it has neither.
- **Requirements flow through annotations.** A collection type reaches the code that looks for its constructors and its `Add` method through a `[DynamicallyAccessedMembers]` type parameter of `XmlMetadataServices`, so the trimmer keeps what is looked up. An array needs nothing looked up and has its own constructor on the collection mapping.

Tests that hold this, all of which fail when the rule is broken:

| Test | What fails it |
| --- | --- |
| `XmlTrimSafetyTests.RuntimeLibrary_HidesNoTrimmingOrAotWarning` | Any `UnconditionalSuppressMessage` in the shipped assembly. |
| `XmlTrimSafetyTests.XmlSerializerMethods_SayTheyNeedReflection_ExactlyWhenTheyTakeNoGeneratedMetadata` | A context overload that gains a warning, or a reflection overload that loses one. |
| `XmlTrimSafetyTests.MembersThatBringInTheReflectionResolver_SayTheyNeedReflection` | An unannotated way to the reflection resolver, or a warning on what generated code uses. |
| `GeneratedCodeChecks`, run by `GeneratorTestHost` on **every** generator run in the suite | Generated source that binds to a member, or a member of a type, that needs reflection; or an `IL` pragma. `GeneratedCodeCheck_FindsEachUseOfAMemberThatNeedsReflection` proves the check sees a method, a property and a type. |
| `XmlAotPublishTests` with `CloudNimble.XmlEssentials.Tests.Aot` | Any trimming or AOT warning from the compiler or from the native compiler (warnings are errors), and any shape that behaves differently in the native binary with reflection off. `CheckTrimmedShapes` covers what the suppressions used to cover: a nullable enumeration, a list of value types, a sorted dictionary, a collection of the program's own, a collection declared by its interface, a stack, mixed content in an array, and a generic closed type. |

---

[Index](README.md) · [Previous: XSD source generation](11-xsd-source-generation.md) · [Next: Repo and packaging](13-repo-and-packaging.md)
