# AOT and trimming

Part of the [XmlEssentials spec](README.md).

On `CloudNimble.XmlEssentials.csproj`:

```xml
<IsAotCompatible>true</IsAotCompatible>
<EnableTrimAnalyzer>true</EnableTrimAnalyzer>
```

- Reflection overloads: `[RequiresDynamicCode]`, `[RequiresUnreferencedCode]`.
- Context overloads: **no** AOT warning.
- Feature switch `IsReflectionEnabledByDefault` trims the reflection resolver.
- Test asset: `CloudNimble.XmlEssentials.Tests.Aot`, published with `PublishAot=true`, `InvariantGlobalization=true`, `IsReflectionEnabledByDefault=false`. Smoke-tests a source-generated `Person`, an XSD-generated `Order`, and a `Utf8XmlReader` walk. CI job `aot` publishes and runs it. This is [PR 8](22-pr-plan.md#pr-8-aot--trimming-tests).
- Sample: `CloudNimble.XmlEssentials.Samples.Cli` (`xmlessentials`) uses McMaster.Extensions.CommandLineUtils (builder API) and the same generated-context path. `PublishAot=true`.

Do not reference `System.Xml.Serialization` or `System.Xml.Schema` from the runtime library — that would drag AOT warnings back in. The assembly-ref test in [packaging](13-repo-and-packaging.md#forbidden-references-ci-test) is the gate (`System.Xml*` and `System.Private.Xml`).

XSD is compile-time only ([11](11-xsd-source-generation.md)). AOT publish must not embed `.xsd` files or resolve schemas. Generated types are registered on a context; `xs:any` is extension data, not `Type.GetType`. PR 8 smoke test includes an XSD-generated type with `IsReflectionEnabledByDefault=false`.

Feature switch XML: [XmlSerializer](07-api-serializer.md#xmlserializer).

---

[Index](README.md) · [Previous: XSD source generation](11-xsd-source-generation.md) · [Next: Repo and packaging](13-repo-and-packaging.md)
