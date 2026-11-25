# XmlEssentials

UTF-8 XML reader, writer, DOM, and AOT-safe serializer for .NET with a System.Text.Json-shaped API. Zero `System.Xml` dependency.

[![NuGet version of XmlEssentials](https://img.shields.io/nuget/v/XmlEssentials?label=NuGet)](https://www.nuget.org/packages/XmlEssentials)
[![Total NuGet downloads of XmlEssentials](https://img.shields.io/nuget/dt/XmlEssentials?label=Downloads)](https://www.nuget.org/packages/XmlEssentials)
[![Build and Deploy workflow status on main](https://img.shields.io/github/actions/workflow/status/CloudNimble/XmlEssentials/build-and-deploy.yml?branch=main&label=Build)](https://github.com/CloudNimble/XmlEssentials/actions/workflows/build-and-deploy.yml)
[![License: MIT](https://img.shields.io/github/license/CloudNimble/XmlEssentials?label=License)](https://github.com/CloudNimble/XmlEssentials/blob/main/LICENSE)

| | |
| --- | --- |
| Package | `XmlEssentials` |
| Assembly | `CloudNimble.XmlEssentials` |
| Namespace | `CloudNimble.Text.Xml` |
| Target frameworks | `net10.0`, `net11.0` |
| Dependencies | None |

## What it does

Each part of XmlEssentials has a System.Text.Json counterpart with the same shape:

| To | Use | The System.Text.Json counterpart |
| --- | --- | --- |
| Turn objects into XML and back | `XmlSerializer` | `JsonSerializer` |
| Read XML one token at a time | `Utf8XmlReader` | `Utf8JsonReader` |
| Write XML without building objects first | `Utf8XmlWriter` | `Utf8JsonWriter` |
| Look up values in a document you don't change | `XmlDocument` | `JsonDocument` |
| Build or edit a document in memory | `XmlNode` | `JsonNode` |

- **No `System.Xml`.** The runtime library doesn't reference `System.Xml` or `System.Private.Xml`, and a test fails if it ever does.
- **Native AOT and trimming.** The package includes a Roslyn source generator. A partial class that derives from `XmlSerializerContext` and names your types with `[XmlSerializable<T>]` gets their serialization code at build time. No trimming or AOT warning is suppressed in the library; the reflection-based overloads are marked `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`.
- **UTF-8 throughout.** Every entry point takes UTF-8 bytes, spans, sequences, and streams. The `string` overloads convert first.
- **Standards.** Well-formed XML 1.0 (Fifth Edition) and XML 1.1 (Second Edition), with Namespaces in XML 1.0 and 1.1. The tests run cases from the W3C XML Conformance Test Suite.
- **Untrusted input.** A document with a `<!DOCTYPE` is rejected, so entity expansion and external entities can't happen. Depth, attribute-count, and size limits are on by default.
- **Existing XML formats.** The `Bcl` preset of `XmlSerializerOptions` reads and writes what `System.Xml.Serialization.XmlSerializer` produces for the same types.

## Install

```bash
dotnet add package XmlEssentials
```

Or in your project file:

```xml
<ItemGroup>
  <PackageReference Include="XmlEssentials" Version="1.*" />
</ItemGroup>
```

The one package contains the library and its source generator. It needs .NET 10 or .NET 11.

## Quickstart

Create a console app with `dotnet new console`, add the package, and replace `Program.cs` with:

```csharp
using CloudNimble.Text.Xml;

var person = new Person { Name = "Ada", Age = 36 };

string xml = XmlSerializer.Serialize(person);
Console.WriteLine(xml);

Person? copy = XmlSerializer.Deserialize<Person>(xml);
Console.WriteLine($"{copy!.Name} is {copy.Age}");

public sealed class Person
{
    public string? Name { get; set; }

    public int Age { get; set; }
}
```

Output:

```text
<?xml version="1.0" encoding="utf-8"?><Person><Name>Ada</Name><Age>36</Age></Person>
Ada is 36
```

Public properties with a getter and a setter become child elements, in the order you declare them. If a file also imports `System.Xml.Serialization`, the name `XmlSerializer` is ambiguous, so remove the `System.Xml` imports from files you move over.

## Usage

### Generate the serializer at build time

Reflection is the default because it needs no setup. For Native AOT or trimmed apps, declare a context and pass its type info:

```csharp
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

string xml = XmlSerializer.Serialize(person, PersonXmlContext.Default.Person);
Person? copy = XmlSerializer.Deserialize(xml, PersonXmlContext.Default.Person);

[XmlSerializable<Person>]
public partial class PersonXmlContext : XmlSerializerContext
{
}
```

### Read tokens from UTF-8

```csharp
ReadOnlySpan<byte> utf8 = "<Order id=\"1042\"><Item>Keyboard</Item><Item>Mouse</Item></Order>"u8;

var reader = new Utf8XmlReader(utf8);
while (reader.Read())
{
    if (reader.TokenType is XmlTokenType.StartElement && reader.LocalNameEquals("Item"u8))
    {
        reader.Read();
        Console.WriteLine(reader.GetString()); // Keyboard, then Mouse
    }
}
```

### Query a parsed document

```csharp
using XmlDocument document = XmlDocument.Parse("""<Order id="1042"><Total>129.90</Total></Order>""");
XmlElement order = document.RootElement;

Console.WriteLine($"Order {order.GetAttribute("id").GetInt32()}: {order.GetChild("Total").GetDecimal()}");
// Order 1042: 129.90
```

Dispose every `XmlDocument`: its memory is rented from a pool. Create `XmlSerializerOptions` once and reuse it, because each instance caches what it learns about your types.

## Performance

Across 67 like-for-like benchmarks against `System.Xml`, XmlEssentials is **3.0× faster on average** and allocates **85% less memory**. It wins 66 of the 67.

| Operation | System.Xml | XmlEssentials | Faster |
| --- | --- | --- | --- |
| Read an `int` | 1.07 µs / 5,968 B | 56 ns / 0 B | 19× |
| Read a customer (object graph) | 8.75 µs / 16,496 B | 2.67 µs / 3,168 B | 3.3× |
| Write 1,000 orders | 425.1 µs / 214,508 B | 201.1 µs / 0 B | 2.1× |
| Parse a 198 KB RSS feed into a DOM | 345.4 µs / 709,083 B | 108.8 µs / 105 B | 3.2× |
| Read a 198 KB RSS feed, token by token | 191.9 µs / 47,137 B | 82.4 µs / 1,441 B | 2.3× |
| Write a 200-item catalog | 33.4 µs / 13,728 B | 16.4 µs / 0 B | 2.0× |

Time / allocated bytes for one operation. One BenchmarkDotNet 0.15.8 run of 229 benchmarks on 2026-11-25: .NET 11 on x64 with the JIT, Windows 11, Intel Core Ultra 9 285K under Hyper-V. Native AOT speed isn't measured. On objects it runs level with System.Text.Json; on single values and the mutable tree it is behind. Every row, the method, and the one comparison `System.Xml` wins are on the [benchmarks page](https://github.com/CloudNimble/XmlEssentials/blob/main/src/CloudNimble.XmlEssentials.Docs/benchmarks.mdx), and the benchmark project is in [`src/CloudNimble.XmlEssentials.Performance`](https://github.com/CloudNimble/XmlEssentials/tree/main/src/CloudNimble.XmlEssentials.Performance).

## Limitations

- UTF-8 only. Documents in other encodings aren't read.
- No DTD processing, and no setting turns it on.
- Generating types from an `.xsd` file is a preview feature and doesn't cover all of XML Schema.

## Resources

- [Documentation source](https://github.com/CloudNimble/XmlEssentials/tree/main/src/CloudNimble.XmlEssentials.Docs): guides for serialization, the reader and writer, both document models, source generation, Native AOT, security, and XML conformance.
- [Migrate from `XmlSerializer`](https://github.com/CloudNimble/XmlEssentials/blob/main/src/CloudNimble.XmlEssentials.Docs/guides/migrate/from-xmlserializer.mdx), [from `XmlReader` and `XmlWriter`](https://github.com/CloudNimble/XmlEssentials/blob/main/src/CloudNimble.XmlEssentials.Docs/guides/migrate/from-xmlreader-xmlwriter.mdx), [from `XDocument`](https://github.com/CloudNimble/XmlEssentials/blob/main/src/CloudNimble.XmlEssentials.Docs/guides/migrate/from-xdocument.mdx), and [from System.Text.Json](https://github.com/CloudNimble/XmlEssentials/blob/main/src/CloudNimble.XmlEssentials.Docs/guides/migrate/from-system-text-json.mdx).
- [Samples](https://github.com/CloudNimble/XmlEssentials/tree/main/src/CloudNimble.XmlEssentials.Samples): the 46 samples the documentation shows, each run by a test.
- [CLI sample](https://github.com/CloudNimble/XmlEssentials/tree/main/src/CloudNimble.XmlEssentials.Samples.Cli): a command-line app that uses a source-generated context and publishes with Native AOT.
- [Implementation contract](https://github.com/CloudNimble/XmlEssentials/blob/main/spec/README.md): the specification and design decisions in `spec/`.
- [Issues](https://github.com/CloudNimble/XmlEssentials/issues): bug reports and questions.
- [DotNetDocs](https://github.com/CloudNimble/DotNetDocs): the CloudNimble SDK that builds this documentation and its API reference.
- [Sponsor CloudNimble](https://github.com/sponsors/cloudnimble) on GitHub Sponsors.

## License

XmlEssentials is released under the [MIT License](https://github.com/CloudNimble/XmlEssentials/blob/main/LICENSE). Copyright © 2026 CloudNimble, Inc.
