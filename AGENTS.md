# General

Please write a high quality, general purpose solution. Implement a solution that works correctly for all valid inputs, not just the test cases. Do not hard-code values or create solutions that only work for specific test inputs. Instead, implement the actual logic that solves the problem generally.

Focus on understanding the problem requirements and implementing the correct algorithm. Tests are there to verify correctness, not to define the solution. Provide a principled implementation that follows best practices and software design principles.

If the task is unreasonable or infeasible, or if any of the tests are incorrect, please tell me. The solution should be robust, maintainable, and extendable.

For maximum efficiency, whenever you need to perform multiple independent operations, invoke all relevant tools simultaneously rather than sequentially. After receiving tool results, carefully reflect on their quality and determine optimal next steps before proceeding. Use your thinking to plan and iterate based on this new information, and then take the best next action.

If you create any temporary new files, scripts, or helper files for iteration, clean up these files by removing them at the end of the task.

## Guidelines

* Make only high confidence suggestions when reviewing code changes.
* Always use the latest version of C#. This repo's `src/global.json` targets .NET 11, so use **C# 15**. When a file-based app or analyzer targets an older TFM, use the language version that TFM supports (`netstandard2.0` generator: C# 12-compatible subset).
* Never change `global.json` unless explicitly asked to.
* Always prefer `.IsNullOrWhiteSpace()` over `.IsNullOrEmpty()` for strings.
* Always prefer `ArgumentException.ThrowIfNullOrWhiteSpace()` over standard string parameter null checks.
* Always prefer `ArgumentNullException.ThrowIfNull()` over standard object parameter null checks.
* Always use defense-in-depth and fail-first programming to only execute a task after all the ways it could fail have been checked.
* Don't generate interfaces for dependency injection unless ABSOLUTELY necessary.
* Prefer C# 15 / modern syntax where it is clearer: collection expressions (`[]`), pattern matching, switch expressions, range expressions, `params` collections, `field` on properties when it removes a manual backing field.
* Zero `System.Xml*` references in the runtime library and unit tests. Coverage scripts and the Performance/Migration projects may use BCL XML.

# Formatting

* Apply code-formatting style defined in `.editorconfig`.
* Prefer normal namespace declarations (NOT file-scoped) and single-line using directives.
* Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
* Ensure that the final return statement of a method is on its own line.
* Organize code into groups surrounded by `#region` in the following order: Fields, Properties, Constructors, Public Methods, Private Methods.
  - Region instructions should be surrounded by blank lines.
* Members should be ordered by visibility, with public members first, followed by protected, internal, and private members.
* Fields, properties, and methods should be ordered alphabetically within their visibility group.
* Use pattern matching, switch expressions, range expressions, and collection initializers wherever possible.
* Use `nameof` instead of string literals when referring to member names.
* Ensure that extensive XML doc comments are created for any APIs.
  * When applicable, include `<example>` and `<code>` documentation in the comments.
  * Only `<param>` tags should be on the same line as content.
  * The `<remarks>` tag should be the last one before the member declaration.

## Nullable Reference Types

* Declare variables non-nullable, and check for `null` at entry points.
* Always use `is null` or `is not null` instead of `== null` or `!= null`.
* Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

# Testing

## Guidelines

* The test project for this library is `CloudNimble.XmlEssentials.Tests` (see `spec/`).
* Run tests from the `/src` folder so `global.json` selects Microsoft.Testing.Platform.
* We use Microsoft.Testing.Platform for test execution, MSTest 4 for test code, Breakdance, and FluentAssertions.
* Always specify `-c Release` or `-c Debug` on `dotnet` commands.
* Do not emit "Act", "Arrange" or "Assert" comments.
* Do not use any mocking in tests. Ever.
* Copy existing style in nearby files for test method names and capitalization.
* Always prefer `.NotBeNullOrWhiteSpace()` over `.NotBeNullOrEmpty()` for testing strings.
* Do not parse expected XML with `XDocument` / `System.Xml` in unit tests.

## Assertions are exact

**Assert the whole value. Never assert that output merely contains, starts with, or ends with something.**

* Output from the writer, the DOM, a node's `ToString()`, a getter, or an exception message is compared with `.Should().Be(...)` against the complete expected string, or with `SequenceEqual` / `.Should().Equal(...)` against the complete expected bytes or token list.
* `.Contain(...)`, `.StartWith(...)`, `.EndWith(...)`, `.Match(...)`, `IndexOf(...) >= 0`, and length-only checks on XML output are **not allowed**. A substring assertion passes when attributes are reordered, namespaces are dropped, whitespace is injected, or text is double-escaped. Every one of those was a real bug in this repo that a `Contain` assertion hid.
* The only exception is a search through a **large fixture document** (tens of kilobytes) for one specific portion. Even then, prefer pinning a count or a SHA-256 digest of the part you care about, as `XmlFixtureDocumentTests` does.
* If the expected value depends on the machine (local UTC offset, current time), compute the expected string in the test and still compare the whole thing. Do not fall back to a prefix.
* When a value is expected to be rejected, assert the exact exception message for well-formedness, encoding, and DTD errors (those messages are the contract). Other `XmlException`s still require the exact type.
* A test named `..._WellFormed...` or `MustRead` must only hold documents that are well formed by the XML specification. If the parser accepts something the specification rejects, fix the parser, not the test. If this spec disagrees with XML 1.0/1.1 or Namespaces, correct this spec (`spec/02-xml-fidelity.md` records the decision). BCL `System.Xml` in .NET 11 is the fallback for developer expectations when the W3C documents are silent.
* **Testing for coverage is not allowed. Every test must be real, at all times.** A test states a behavior a caller depends on and asserts the exact result. Do not write a test, or add inputs to an existing one, in order to reach a line or branch. When the coverage gate reports a gap: if a meaningful input reaches it, test that behavior; if it is an internal precondition, test it in `*InternalsTests`; if it has no caller outside tests, it is dead code, so delete it. Never exclude code from coverage or lower the gate. See rule 5 in `spec/14-testing.md`.
* **A check on an internal precondition (a depth, an index, a count, an unknown enum value) is defense in depth, not dead code.** It is never removed because coverage does not reach it. Cover it with a test that calls the internal member, in an `*InternalsTests` class, with an exact assertion. Those tests are the record of why the check exists. `Debug.Assert` is the wrong substitute: it vanishes in Release.
* Dead code means a member with no caller outside tests, shown by a search of every project in `/src`. Never delete a test and the code it tests in the same change. Never restructure product code so the coverage tool reports a different branch. When the gate reports a gap you believe is unreachable, stop and ask.
* **Specifications first.** Behavior follows XML 1.0 Fifth Edition, XML 1.1 Second Edition, Namespaces in XML 1.0 and 1.1, and XML Schema Part 2, in that order. `spec/02-xml-fidelity.md` is the project spec and is corrected when it disagrees with those documents. A test may not encode behavior that contradicts them.

## What a change must be tested against

* **Specifications first.** See the testing guidelines above. `spec/02-xml-fidelity.md` records every deliberate departure.
* **W3C conformance.** `XmlConformanceSuiteTests` runs the W3C XML Conformance Test Suite manifests in `Fixtures/w3c-xmlconf`. A reader change must keep every case passing.
* **Real documents.** `XmlFixtureDocumentTests` reads documents taken from other parsers' test suites and compares them with counts and digests produced by an independent reader. Add a fixture (and its `Fixtures/README.md` entry) when a new document shape matters.
* **Properties, not just examples.** Reader changes must hold under `Utf8XmlReaderSplitTests` (every byte offset) and the chunked reads in the fixture tests. Writer and DOM changes must hold under `XmlRoundTripTests` and `XmlWriterStyleTests`: what is written is read back exactly; what is parsed is written back with the same infoset, and byte for byte when it is already in the form the writer's options ask for; and anything else that is rewritten is rewritten by a named setting, the same way everywhere (`spec/02-xml-fidelity.md`, round-trip guarantees).
* **Both XML versions.** A fast path in the reader or writer (a bulk scan, a bulk copy, a skipped check) is tested under XML 1.0 and XML 1.1, in text and in attribute values. XML 1.1 treats C1 controls, NEL (U+0085) and LS (U+2028) differently, and their UTF-8 lead bytes (`0xC2`, `0xE2`) pass a scan written with only 1.0 in mind.
* Typed values go through `XmlLexical` only. Do not parse a value anywhere else; add the case to `XmlLexicalTests`, which runs it through the reader and the DOM together.
* Baseline testing: `D:\GitHub\Breakdance\specs\baseline-testing.md`
  * Add `<AssemblyAttribute Include="CloudNimble.Breakdance.Assemblies.BreakdanceTestAssembly" />` to the test csproj.
  * Use `[BreakdanceManifestGenerator]` methods and `dotnet breakdance generate` to create or update baselines.
  * If tests that need the baselines fail, and you regenerated the baselines, then the code to generate them is wrong and you need to fix it.

## Coverage gate

The gate fails below **100% line** or **100% branch** on `CloudNimble.XmlEssentials.dll`. Run both commands from `/src` before calling a change done, and before writing a coverage number anywhere:

```bash
dotnet test CloudNimble.XmlEssentials.slnx -c Release --coverage --coverage-output-format cobertura --coverage-output <absolute path>/artifacts/coverage/coverage.cobertura.xml
dotnet run coverage-gate.cs -- artifacts/coverage/coverage.cobertura.xml
```

* Report the numbers the gate prints. Do not infer coverage from the tests passing.
* A gap is closed by testing the real behavior, testing an internal precondition in `*InternalsTests`, or deleting a member that has no caller outside tests. Never by a coverage-only test, an exclusion, a lower threshold, or by removing a guard because the tool did not reach it.

# Performance

The process and the current tables are in `spec/23-optimization.md`. Read it before any speed or allocation work.

* **Measure first.** Profile the real workload (`dotnet-trace collect --profile cpu-sampling`) before changing code, and benchmark after. Do not state a cause or a win that a profile or benchmark has not shown.
* Run benchmarks from `/src`: `dotnet run -c Release --project CloudNimble.XmlEssentials.Performance -f net11.0 -- --filter "*"`.
* A 0 B claim needs a `GC.GetAllocatedBytesForCurrentThread` test in `*AllocationTests`. A benchmark column is not enough.
* Never special-case a benchmark payload. A fast path must be correct and useful for every valid input.
* Do not grow the public API for speed unless the benchmark cannot express the fast path without it.

# Keeping the plan current

* A change that finishes a PR, a backlog item, or an open question updates `spec/22-pr-plan.md`, `spec/23-optimization.md`, and `spec/20-open-questions.md` in the same commit. Regenerate benchmark tables and coverage counts there when they change.
* When a rule changes (a gate threshold, a process), fix every statement of the old rule across `/spec` and this file, not just the first one found.

# Documentation

## Source System

* We're using Mintlify.com for documentation.
  * User instructions: https://mintlify.com/docs/llms-full.txt
  * Schema for docs.json: https://leaves.mintlify.com/schema/docs.json

## Process

* The documentation is built by `dotnet build` (with `-c Release` or `-c Debug`). The docs project (DotNetDocs.Sdk) turns the C# doc XML files into Mintlify-Enhanced Markdown and generates `docs.json` as part of that build.
* **Ignore `docs.json`.** Never edit it by hand, never restore or "fix" its contents, and do not treat a change the build makes to it as a regression. Whatever the build produces is correct.
* **Placeholder files are fine.** The build creates a set of `.mdz` files under `conceptual/` for every type, each starting with a `TODO: REMOVE THIS COMMENT` line. DotNetDocs ignores a file that still has that line, so it never reaches the published documentation. Write a section when the type needs it; otherwise leave the placeholder. Do not report placeholders as unfinished work, count them, or ask whether to fill them.
* **Document what exists, not what was removed.** The guides, the conceptual notes and the XML doc comments describe the library as a reader finds it. Do not add a note about a member, option or behavior that the reader would never know existed: something that was removed, renamed, or never shipped. "There is no `X`" and "unlike before, ..." are noise to someone who never saw `X`. When a member is removed, delete what was written about it; do not replace it with an explanation of its absence. The record of a removal belongs in `/spec`. The one exception is a page whose subject is a comparison with another API (a migration guide, the proposal comparison): there, saying that the other API's member has no counterpart is the point.
* XML documentation comments must be consistent, succinct, and as useful as possible.

# Build Process

* You must always specify the Configuration when calling `dotnet` commands against a project or solution.

## Important Windows File System Notes

* NEVER redirect output to `nul` on Windows. The string "nul" is a reserved device name in Windows and will create an undeletable file.
  * Instead of `> nul`, use `> $null` in PowerShell or `> NUL` (uppercase) in cmd.
  * Better yet, avoid output redirection entirely when not necessary.
* When checking if files/directories exist, use proper error handling instead of redirecting to nul.

# Project Details

* Implementation contract: `/spec`.
* Package id `XmlEssentials`. Assembly `CloudNimble.XmlEssentials`. Namespace `CloudNimble.Text.Xml`.

# Final Thoughts

You can do it! Don't hold back. Give it your all.
