# xmlessentials CLI sample

Parse and serialize UTF-8 XML with XmlEssentials. Commands use the McMaster builder API. The serializer uses a source-generated `XmlSerializerContext` so Native AOT does not need reflection.

```bash
dotnet run -c Release --project CloudNimble.XmlEssentials.Samples.Cli -- serialize --name Ada --age 36
dotnet run -c Release --project CloudNimble.XmlEssentials.Samples.Cli -- parse catalog.xml
dotnet run -c Release --project CloudNimble.XmlEssentials.Samples.Cli -- roundtrip person.xml
```

Native AOT (`PublishAot` is on the project):

```bash
dotnet publish CloudNimble.XmlEssentials.Samples.Cli -c Release
```

Commands:

| Command | What it does |
| --- | --- |
| `serialize` | Writes a `Person` through `AppXmlContext.Default.Person`. |
| `parse` | Walks a file with `Utf8XmlReader` and prints start-element, attribute, and text counts. |
| `roundtrip` | Deserializes a `Person` file and writes it back. |
