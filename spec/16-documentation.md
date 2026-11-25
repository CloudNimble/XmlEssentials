# Documentation

Part of the [XmlEssentials spec](README.md).

DotNetDocs.Sdk / Mintlify.

- Project: **`CloudNimble.XmlEssentials.Docs.docsproj`**.
- `IsDocumentationProject=true`
- API reference is generated from the XML documentation comments by `dotnet build`.
- Conceptual sections per namespace and type are the `.mdz` files under `conceptual/` (`summary`, `usage`, `examples`, `best-practices`, `patterns`, `considerations`, `related-apis`). The build merges them into the API reference pages.
- Root pages are `.mdx` files in the project folder. Navigation is declared in the `.docsproj` (`MintlifyTemplate/Navigation`). `docs.json` is generated; never edit it.

## Goal

The documentation should read like a Microsoft product, because the aim is for Microsoft to adopt the library. It is where developers and AI agents learn what the library does and what to expect from it. Reference text follows Microsoft's API reference guidelines; conceptual pages follow the .NET docs voice and the shape of the System.Text.Json documentation set.

## The documentation phase

Work the steps in order.

1. **XML documentation comments.** Correct every public type and member to the API reference rules below. Wording only: no signature, behavior, or visibility changes.
2. **Conceptual documentation.** Go through every `.mdz` file and extend it with complete samples, best practices, and the considerations a reader needs, as the Microsoft docs do.
3. **New guides.** How-to guides and migration guides under `guides/`.
4. **Root index.** A developer welcome: "the XML API you've been begging for", the direct comparison with System.Text.Json (speed, source generation, AOT, XML 1.1 support), and where to start.
5. **Quickstart.** Installing the package in an application and the basics, with best practices.
6. **Why XmlEssentials.** The backstory of [dotnet/runtime#125299](https://github.com/dotnet/runtime/issues/125299) and of how long .NET developers have asked for this.
7. **Benchmarks page.** A new root page that summarizes the benchmarks in plain numbers with short explanations.
8. **Mintlify features.** Code blocks for every example in `.mdx` and `.mdz` files, `Steps` for procedures, cards, icons, and color where they help. Do not hide content behind extra clicks.

## Decisions (2026-11-23)

- **Samples live in a project.** `CloudNimble.XmlEssentials.Samples` holds one numbered class file per sample (`01_Basics.cs`, and so on). Each docs page that shows a sample links to its file. `CloudNimble.XmlEssentials.Tests.Samples` calls every sample and asserts the output the page shows.
- **Full pass.** The root pages are rewritten from scratch. An `.mdz` file that already has useful detail keeps it.
- **Not every `.mdz` file is filled in.** Write a section only where it earns its place; not every class needs best practices.
- **Pages beyond the eight steps:** supported types, security, thread safety, source-generation modes, custom converters, polymorphism, XML conformance, and a map from the `System.Text.Xml` proposal's names to this library's.
- **Migration guides:** from `System.Xml.Serialization.XmlSerializer`, from `XmlReader` / `XmlWriter`, from `XDocument` / `XmlDocument`, and coming from System.Text.Json.
- **Why XmlEssentials** draws on the whole history of the request (other runtime issues, community threads), not only #125299.
- **Version:** the packages are 1.0.0 RTM. Install with `dotnet add package XmlEssentials`.
- **Navigation** for every new page is added in the `.docsproj`.
- **Out of scope:** the repository `README.md` and the package README (drafted elsewhere first), and an agents page (assess after Mintlify generates `llms.txt`).

## How the pieces are built

- **Samples.** One file per sample in `src/CloudNimble.XmlEssentials.Samples`, named `NN_Name.cs`, holding one static class with `public static void Run(TextWriter output)` and further methods for further snippets. Model types are in `Models/<Area>/`. Numbers: 01 to 05 getting started, 06 to 13 serialization, 14 to 19 shapes, 20 to 25 reader and writer, 26 to 30 DOM, 31 to 36 source generation and AOT, 37 to 39 reference, 40 to 45 migration.
- **Sample tests.** `src/CloudNimble.XmlEssentials.Tests.Samples` has one test class per area. Each test captures what a sample method prints (`SampleOutput.Capture`) and compares it with the complete text the page shows. The tests run under the invariant culture (`SampleTestSetup`), so the printed numbers are the same on every machine.
- **Pages.** A code block on a page is copied from a sample method, the output block is the string its test asserts, and the page links to the sample file on GitHub.
- **Conceptual files.** An `.mdz` file has no heading of its own, because the build adds `## Usage`, `## Examples`, and so on; subheadings inside it are `###`. `related-apis.mdz` is one fully qualified type name per line and nothing else. A section with nothing to say is left as the DotNetDocs placeholder (the file that starts with the `TODO` comment), which the build leaves out while `ShowPlaceholders` is `false`. Do not delete the file.
- **Checks.** `dotnet build` of the docs project regenerates the API reference and `docs.json`; `mint broken-links` and `mint validate` in the docs folder must pass.

## Rules for every page

- **Every claim is backed.** A performance statement cites a row of [benchmarks](15-benchmarks.md) or [optimization](23-optimization.md). XmlEssentials is faster than `System.Xml` on every measured row; it is not faster than System.Text.Json on most rows, and no page says it is.
- **Every sample is real.** A sample uses the public API as it exists and shows the output it produces.
- **No stale statements.** A page never describes a feature as missing after it ships (the first quickstart said `XmlSerializer` was not in the release).

## XML documentation comments: the rule sheet

Source: the [dotnet-api-docs wiki](https://github.com/dotnet/dotnet-api-docs/wiki) and [Recommended XML tags](https://learn.microsoft.com/dotnet/csharp/language-reference/xmldoc/recommended-tags), read 2026-11-23. The wiki shows `true`, `false`, and `null` in bold; in a doc comment that is `<see langword="true"/>`, `<see langword="false"/>`, `<see langword="null"/>`.

General: complete sentences that end with a period. Present tense, third person. Do not name the type in a parameter or return description, and do not just repeat the member's name.

### Summaries

| Member | Template |
| --- | --- |
| Class, struct | Begins with a present-tense verb: "Represents ...", "Provides ...". |
| Static or sealed class | The same, then "This class cannot be inherited." |
| Abstract base class | "Defines the core behavior of ... and provides a base for ...". |
| Interface | "Defines ...", "Provides ...", or "Exposes ...". |
| Exception class | "The exception that is thrown when ...". |
| Enum | "Specifies ..." or "Describes ...". |
| Enum member | A noun phrase: "A single-line border." |
| Field, constant | "Specifies ..." or "Represents ...". |
| Constructor (class) | "Initializes a new instance of the `<Class>` class." An overload adds how: "... using the specified options." |
| Constructor (struct) | "Initializes a new instance of the `<Struct>` struct." |
| Constructor (abstract class) | "Called from constructors in derived classes to initialize the `<Class>` class." |
| Property, read/write | "Gets or sets ...". Boolean: "Gets or sets a value that indicates whether ...". |
| Property, read/init | "Gets or initializes ...". Boolean: "Gets or initializes a value that indicates whether ...". |
| Property, read-only | "Gets ...". Boolean: "Gets a value that indicates whether ...". Do not add "This property is read-only." |
| Method | Begins with a present-tense verb: "Reads the next token ...". |
| Task-returning method | "Asynchronously `<verb>` ...". |
| `Dispose()` | "Releases the resources used by the current instance of the `<Class>` class." |
| Abstract member | "When overridden in a derived class, `<verb>` ...". |
| Method that always throws | "Throws a `<Exception>` exception in all cases." Remarks say why. |
| Operator | Begins with a verb. Conversion: "Converts a `<Type>` to a `<Type>`." |
| Overloads | Each overload's summary says what sets it apart by its parameters. |
| Explicit interface member | Copy the interface member's text. |

### Parameters

| Kind | Template |
| --- | --- |
| Boolean | "`true` to ...; otherwise, `false`." Both stated only when needed: "`true` to ...; `false` to ...". |
| `out` | "When this method returns, contains ... . This parameter is treated as uninitialized." |
| `ref` | "The ..., passed by reference." |
| Class, struct, interface | A noun phrase with an article: "The buffer to write to." |
| Enum | "One of the enumeration values that ...". Flags: "A bitwise combination of the enumeration values that ...". |
| Primitive or string | A noun phrase with an article: "The size of the buffer, in bytes." State units, limits, the default, and whether `null` is allowed. |
| Index | "The zero-based index of ...". |
| Array | "An array that contains ...". |

### Return values and property values

| Kind | Template |
| --- | --- |
| Boolean return | "`true` if ...; otherwise, `false`." |
| Boolean property (`<value>`) | "`true` if ...; otherwise, `false`. The default is `false`." |
| `Task<T>` / `ValueTask<T>` | "A task object that, when awaited, produces ...". |
| Object | A noun phrase with an article: "The new element." |
| Enum | "One of the enumeration values that ...". A property adds "The default is `<Member>`." |
| Primitive or string | A noun phrase: "The number of bytes written." A property adds the default when one matters. |
| Method that always throws | "This method doesn't return a value." |

The wiki has no template for the `Try` pattern. Use the Boolean return and `out` templates together.

### Exceptions

- The text is the condition, written as if "if" came before it, in the present tense: "`utf8Xml` is not well formed."
- Several conditions: one sentence each, joined by "-or-" on its own line.
- Thrown from a task: add "This exception is stored into the returned task."
- Always thrown: "In all cases."

### Remarks and tags

- Remarks hold what the summary, parameters, and return value cannot: usage detail, defaults tables, why a member always throws. Do not bury what a caller must know there; state it in the parameter or summary and point to the remarks.
- `<summary>` on every public type and member. `<value>` on properties. `<returns>` on methods. `<param>` and `<typeparam>` for every parameter. `<exception cref>` for each exception thrown directly.
- `<paramref>`, `<typeparamref>`, `<see cref>`, `<see langword>` in text. `<see href>` for an external link. `<c>` for inline code, `<example>` with `<code>` for samples.
- `<inheritdoc/>` on overrides and interface implementations.
- Repository rules still apply ([AGENTS.md](../AGENTS.md)): only `<param>` tags share a line with their content, and `<remarks>` is the last tag before the member.

## Conceptual pages: voice and style

Source: [.NET docs voice and tone](https://learn.microsoft.com/contribute/dotnet/dotnet-voice-tone), the [.NET docs style guide](https://learn.microsoft.com/contribute/dotnet/dotnet-style-guide), and the Microsoft Writing Style Guide ([top tips](https://learn.microsoft.com/style-guide/top-10-tips-style-voice), [step-by-step instructions](https://learn.microsoft.com/style-guide/procedures-instructions/writing-step-by-step-instructions), [headings](https://learn.microsoft.com/style-guide/scannable-content/headings)), read 2026-11-23.

- Conversational, second person ("you"), active voice, present tense. Avoid the future tense.
- Lead with what matters. Short sentences. Contractions are fine.
- Say what a thing is before saying why it matters. Define uncommon terms.
- Headings use sentence-style capitalization and have no closing punctuation. Keep them short and parallel within a level. No two headings in a row without text between them.
- Use the serial comma.
- Procedures are numbered steps: one action per step, each a complete sentence that starts with an imperative verb, and say where before what.
- Code font for keywords, type and member names, package names, and commands. Italic for file names and new terms.
- Page title of 60 characters or fewer; description of 115 to 145 characters.
- Samples come from working code.

## Structure to mirror

The System.Text.Json documentation set on Microsoft Learn, read 2026-11-23:

- **Overview**: how to get the library, namespaces and APIs, reflection vs. source generation, security information, thread safety.
- **How to**: serialize; deserialize; customize names and values; ignore properties; required properties; unmapped members; immutable types; configure options; references; polymorphism; use the DOM; use the writer; use the reader.
- **Migrate**: a table of differences, differences in default behavior, scenarios, then the DOM, reader, and writer compared with their older counterparts.
- **Supported types.**
- **Advanced**: reflection vs. source generation, source-generation modes, use source generation, custom converters, customize contracts.

---

[Index](README.md) · [Previous: Benchmarks](15-benchmarks.md) · [Next: Alternatives](17-alternatives.md)
