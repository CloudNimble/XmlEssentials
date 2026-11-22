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
* Baseline testing: `D:\GitHub\Breakdance\specs\baseline-testing.md`
  * Add `<AssemblyAttribute Include="CloudNimble.Breakdance.Assemblies.BreakdanceTestAssembly" />` to the test csproj.
  * Use `[BreakdanceManifestGenerator]` methods and `dotnet breakdance generate` to create or update baselines.
  * If tests that need the baselines fail, and you regenerated the baselines, then the code to generate them is wrong and you need to fix it.

# Documentation

## Source System

* We're using Mintlify.com for documentation.
  * User instructions: https://mintlify.com/docs/llms-full.txt
  * Schema for docs.json: https://leaves.mintlify.com/schema/docs.json

## Process

* API documentation is created automatically by using the `dotnet easyaf mintlify` command (DotNetDocs.Sdk) to turn C# doc XML files into Mintlify-Enhanced Markdown.
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
