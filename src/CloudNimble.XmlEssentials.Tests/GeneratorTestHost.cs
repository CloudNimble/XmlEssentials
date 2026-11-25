using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.SourceGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Runs <see cref="SerializerGenerator"/> and <see cref="XsdGenerator"/> against a snippet and returns the compilation result.
    /// </summary>
    internal static class GeneratorTestHost
    {

        #region Public Methods

        /// <summary>
        /// Runs both generators on <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The consumer C#.</param>
        /// <returns>The updated compilation, diagnostics, and generated trees.</returns>
        internal static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<SyntaxTree> Generated) Run(string source)
        {
            return Run(source, [], null);
        }

        /// <summary>
        /// Runs both generators on <paramref name="source"/> with AdditionalFiles.
        /// </summary>
        /// <param name="source">The consumer C#.</param>
        /// <param name="additionalTexts">The AdditionalFiles.</param>
        /// <param name="optionsProvider">Optional analyzer config (AdditionalFile metadata and global options).</param>
        /// <returns>The updated compilation, diagnostics, and generated trees.</returns>
        internal static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<SyntaxTree> Generated) Run(
            string source,
            IEnumerable<AdditionalText> additionalTexts,
            AnalyzerConfigOptionsProvider? optionsProvider)
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Preview)
                .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "CloudNimble.Text.Xml.Generated")]);
            var compilation = CSharpCompilation.Create(
                "GeneratorTests",
                [CSharpSyntaxTree.ParseText(SourceText.From(source), parseOptions)],
                MetadataReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var texts = additionalTexts as AdditionalText[] ?? [.. additionalTexts];
            var driver = CSharpGeneratorDriver.Create(
                [new SerializerGenerator().AsSourceGenerator(), new XsdGenerator().AsSourceGenerator()],
                additionalTexts: texts,
                parseOptions: parseOptions,
                optionsProvider: optionsProvider);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
            var generated = output.SyntaxTrees.Where(tree => tree != compilation.SyntaxTrees.First()).ToImmutableArray();
            // Whatever a test is about, nothing the generators emit may use a member that needs reflection.
            GeneratedCodeChecks.MustNeedNoReflection(output, generated);
            return (output, diagnostics.AddRange(output.GetDiagnostics()), generated);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns metadata references for the generator host compilation.
        /// </summary>
        /// <returns>The references.</returns>
        internal static IEnumerable<MetadataReference> MetadataReferences()
        {
            var assemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
            foreach (var path in assemblies.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                yield return MetadataReference.CreateFromFile(path);
            }

            yield return MetadataReference.CreateFromFile(typeof(XmlSerializer).Assembly.Location);
        }

        #endregion

    }

}
