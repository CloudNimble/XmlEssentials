# Repo and packaging

Part of the [XmlEssentials spec](README.md).

Follow `D:\GitHub\ClaudeEssentials`, with the **required** differences below. Do not copy IVT/`IsPrimaryProject` blindly.

```
D:\GitHub\XmlEssentials\
  LICENSE
  README.md
  spec\
    README.md
    00-overview.md … 23-optimization.md
  src\
    Directory.Build.props
    Directory.Build.targets
    global.json                    # SDK 11.0.x, test.runner: Microsoft.Testing.Platform
    coverage-gate.cs               # MTP coverage threshold (C# file-based app)
    .editorconfig
    CloudNimble.XmlEssentials.slnx
    CloudNimble.XmlEssentials\
    CloudNimble.XmlEssentials.SourceGeneration\
    CloudNimble.XmlEssentials.Tests\
    CloudNimble.XmlEssentials.Performance\
    CloudNimble.XmlEssentials.Docs\
    CloudNimble.XmlEssentials.Samples\
```

Canonical tree is already `CloudNimble.XmlEssentials.*`. Namespace stays `CloudNimble.Text.Xml`.

## Directory.Build.props (contract)

```xml
<PackageId>$(MSBuildProjectName.Replace('CloudNimble.', ''))</PackageId>
<Product>XmlEssentials</Product>
<Authors>CloudNimble, Inc.</Authors>
<Company>CloudNimble, Inc.</Company>
<Copyright>Copyright © 2026 CloudNimble, Inc. All rights reserved.</Copyright>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
<PackageProjectUrl>https://github.com/CloudNimble/XmlEssentials</PackageProjectUrl>
<RepositoryUrl>https://github.com/CloudNimble/XmlEssentials.git</RepositoryUrl>
<PackageTags>xml;serializer;aot;utf8;source-generator;cloudnimble;xmlessentials</PackageTags>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<StandardTestTfms>net11.0;net10.0</StandardTestTfms>
```

Do **not** set `PackageIcon` / `PackageIconUrl`. Do **not** invent a docs hostname for `PackageProjectUrl`; the GitHub repo URL is sufficient. Docs pages that need a site URL use `(docs site TBD)`.

Project-kind flags (set before `IsPrimaryProject`):

```xml
<IsSourceGeneratorProject Condition="$(MSBuildProjectName.EndsWith('.SourceGeneration'))">true</IsSourceGeneratorProject>
<IsDocumentationProject Condition="$(MSBuildProjectName.EndsWith('.Docs'))">true</IsDocumentationProject>
<IsBenchmarkProject Condition="$(MSBuildProjectName.EndsWith('.Performance'))">true</IsBenchmarkProject>
<IsTestProject Condition="$(MSBuildProjectName.Contains('.Tests'))">true</IsTestProject>
<IsSampleProject Condition="$(MSBuildProjectName.Contains('.Samples'))">true</IsSampleProject>
<IsPrimaryProject Condition="'$(IsSourceGeneratorProject)' != 'true' And '$(IsDocumentationProject)' != 'true' And '$(IsBenchmarkProject)' != 'true' And '$(IsTestProject)' != 'true' And '$(IsSampleProject)' != 'true'">true</IsPrimaryProject>
```

Also:

- `IncludeSymbols=true`, `snupkg`, `PublishRepositoryUrl`, SHA256 checksums.
- Primary projects: `GenerateDocumentationFile`, `GeneratePackageOnBuild`.
- `DocumentationFile` in **Directory.Build.targets**.
- Tests: `EnableMSTestRunner`, `MSTest` 4.*, `FluentAssertions` 6.*, `Microsoft.Testing.Extensions.CodeCoverage` (MTP coverage). `coverlet.collector` may remain for VS Test Explorer **only** — it is **not** the CI fail gate under MTP. `Parallelize` method-level.
- **IVT only on the runtime assembly**, exact: `CloudNimble.XmlEssentials` → `CloudNimble.XmlEssentials.Tests`.
- Runtime: `IsAotCompatible` / `EnableTrimAnalyzer`, `Nullable`/`ImplicitUsings` enable.
- Generator: `IsPackable=false` (redundant if not primary).
- Docs: `IsDocumentationProject=true`.
- CS1591: `NoWarn` on empty projects; **removed for the runtime library in PR 2**.
- **Zero** `PackageReference` to `System.Xml*`.

`global.json` (match ClaudeEssentials):

```json
{
  "sdk": { "version": "11.0.100", "rollForward": "latestPatch", "allowPrerelease": false },
  "test": { "runner": "Microsoft.Testing.Platform" }
}
```

## Coverage fail gate (MTP — not coverlet.runsettings)

**Host is Microsoft.Testing.Platform.** VSTest DataCollectors (`coverlet.collector` `friendlyName="XPlat Code Coverage"`, `--settings coverlet.runsettings`) **do not run** under MTP. Do not claim they fail the build.

**Collection:** `Microsoft.Testing.Extensions.CodeCoverage` on the test project.

**Exact CI commands** (GitHub Actions, same process as McpEssentials: `pr-validation.yml`, `build-and-deploy.yml`, `publish-ci-build.yml`):

```bash
# from src/, so global.json selects Microsoft.Testing.Platform
dotnet test CloudNimble.XmlEssentials.slnx -c Release --no-build -f net10.0
dotnet test --project CloudNimble.XmlEssentials.Tests.Samples/CloudNimble.XmlEssentials.Tests.Samples.csproj -c Release --no-build -f net11.0
dotnet test --project CloudNimble.XmlEssentials.Tests/CloudNimble.XmlEssentials.Tests.csproj -c Release --no-build -f net11.0 --coverage --coverage-output-format cobertura --coverage-output "$GITHUB_WORKSPACE/artifacts/coverage/coverage.cobertura.xml"
dotnet run coverage-gate.cs -- "$GITHUB_WORKSPACE/artifacts/coverage/coverage.cobertura.xml"
```

Only `CloudNimble.XmlEssentials.Tests` collects coverage. Every test host given `--coverage-output` writes its own Cobertura report to that one path (a relative path resolves under the shared `src/TestResults/`, not per host), so a solution-wide coverage run leaves the gate reading whichever host finished last: the samples host alone covers about 81% of the library. The samples tests still run, without coverage; the number the gate reads is the library's own test project on net11.0.

net11.0 also runs `XmlAotPublishTests` (Native AOT publish + exact Person/Order XML). The workflows then publish the `xmlessentials` CLI sample. Version stamps, NuGet Trusted Publishing, and GitHub releases: [`.github/RELEASE.md`](../.github/RELEASE.md).

(`dotnet run file.cs` is a .NET 10+ C# file-based app. **Not** Python. **Not** `dotnet test --settings`.)

`Microsoft.Testing.Extensions.CodeCoverage` **does not** document a fail-the-build line-100% threshold equivalent to coverlet’s DataCollector `<Threshold>`. The extension **collects** Cobertura; it does **not** fail CI on coverage. The fail gate is **`coverage-gate.cs`**, which:

1. Parses the Cobertura XML.
2. Filters assembly `CloudNimble.XmlEssentials` (include `[CloudNimble.XmlEssentials]*`; exclude tests, generator, benches).
3. **Fails** if line-rate &lt; 1.00 (100% line).
4. **Reports** branch-rate; **fails** if branch-rate &lt; 0.80 unless the file is on the documented tokenizer branch-exclusion list. Never exclude the tokenizer from **line**.
5. On a library with **zero** instrumented lines (PR 1 empty lib), exit 0 after proving the Cobertura file exists (collector invoked). Line 100% becomes a real fail from **PR 2**.

Do **not** check in `coverlet.runsettings` as the CI gate. Do **not** set coverlet.msbuild `<Threshold>` properties.

## Breakdance

```xml
<PackageReference Include="Breakdance.Assemblies" Version="8.*" />
```

NuGet id is **`Breakdance.Assemblies`**, not `CloudNimble.Breakdance.Assemblies`. Assembly namespace remains `CloudNimble.Breakdance.Assemblies`. Local Breakdance repo is `9.0.0-preview.1`; tests use 8.* to match EasyAF.

Checked-in public API reports under `src/CloudNimble.XmlEssentials.Tests/Baselines/`. Update the baseline **in the same PR** that changes the public surface.

## slnx folders

`/Core/`, `/Docs/`, `/Spec/`, `/Samples/`, `/Solution Items/`, `/Tests/`, `/Benchmarks/`. The `/Spec/` folder lists every file under `../spec/`.

## Runtime csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net11.0;net10.0</TargetFrameworks>
    <IsAotCompatible>true</IsAotCompatible>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
    <PackageId>XmlEssentials</PackageId>
    <Description>UTF-8 XML reader, writer, DOM, and AOT-safe serializer with a System.Text.Json-shaped API. Zero System.Xml dependency.</Description>
    <RootNamespace>CloudNimble.Text.Xml</RootNamespace>
    <AssemblyName>CloudNimble.XmlEssentials</AssemblyName>
  </PropertyGroup>
</Project>
```

Generator pack `ItemGroup`: [source generation](10-source-generation.md#nupkg-layout).

## Forbidden references (CI test)

**Source of truth:** `Assembly.GetReferencedAssemblies()` on `CloudNimble.XmlEssentials.dll` — no name starts with `System.Xml` and no `System.Private.Xml`. On net10/net11, `System.Xml.Serialization` is **framework**, not a PackageReference. Ban `using System.Xml` in the runtime library and unit tests. BCL XML is allowed **only** in `CloudNimble.XmlEssentials.Performance` and an optional `CloudNimble.XmlEssentials.Tests.Migration` project.

---

[Index](README.md) · [Previous: AOT and trimming](12-aot-and-trimming.md) · [Next: Testing](14-testing.md)
