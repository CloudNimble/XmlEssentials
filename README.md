# XmlEssentials

UTF-8 XML reader, writer, DOM, and AOT-safe serializer with a System.Text.Json-shaped API. Zero `System.Xml` dependency.

| | |
|---|---|
| Package | `XmlEssentials` |
| Assembly | `CloudNimble.XmlEssentials` |
| Namespace | `CloudNimble.Text.Xml` |

```bash
dotnet add package XmlEssentials
```

```csharp
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

string xml = XmlSerializer.Serialize(person);
Person? p = XmlSerializer.Deserialize<Person>(xml);
```

## Performance

Across 67 like-for-like benchmarks against `System.Xml`, XmlEssentials is **3.0× faster on average** and allocates **85% less memory**. It wins 66 of the 67.

| Operation | System.Xml | XmlEssentials | Faster |
|---|---|---|---|
| Read an `int` | 1.07 µs / 5,968 B | 56 ns / 0 B | 19× |
| Read a customer (object graph) | 8.75 µs / 16,496 B | 2.67 µs / 3,168 B | 3.3× |
| Write 1,000 orders | 425.1 µs / 214,508 B | 201.1 µs / 0 B | 2.1× |
| Parse a 198 KB RSS feed into a DOM | 345.4 µs / 709,083 B | 108.8 µs / 105 B | 3.2× |
| Read a 198 KB RSS feed, token by token | 191.9 µs / 47,137 B | 82.4 µs / 1,441 B | 2.3× |
| Write a 200-item catalog | 33.4 µs / 13,728 B | 16.4 µs / 0 B | 2.0× |

Time / allocated bytes for one operation, .NET 11, BenchmarkDotNet. On lists and graphs of objects it runs level with System.Text.Json. Every row, the method, and the one comparison `System.Xml` wins are on the [benchmarks page](https://github.com/CloudNimble/XmlEssentials/blob/main/src/CloudNimble.XmlEssentials.Docs/benchmarks.mdx), and the benchmark project is in [`src/CloudNimble.XmlEssentials.Performance`](https://github.com/CloudNimble/XmlEssentials/tree/main/src/CloudNimble.XmlEssentials.Performance).

Implementation contract: [`spec/`](https://github.com/CloudNimble/XmlEssentials/blob/main/spec/README.md).
