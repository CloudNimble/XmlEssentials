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

Implementation contract: [`spec/`](spec/README.md).
