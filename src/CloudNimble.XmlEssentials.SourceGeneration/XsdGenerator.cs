using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// Incremental source generator that turns AdditionalFiles <c>*.xsd</c> into POCOs, a context, and AOT converters.
    /// </summary>
    /// <remarks>
    /// Runs in the same analyzer DLL as <see cref="SerializerGenerator"/>. It calls the shared emit APIs so a single
    /// <c>dotnet build</c> produces serialize/deserialize implementations without seeing another generator's output.
    /// </remarks>
    [Generator(LanguageNames.CSharp)]
    public sealed class XsdGenerator : IIncrementalGenerator
    {

        #region Public Methods

        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var files = context.AdditionalTextsProvider
                .Where(static text => text.Path.EndsWith(".xsd", StringComparison.OrdinalIgnoreCase))
                .Combine(context.AnalyzerConfigOptionsProvider)
                .Select(static (pair, token) =>
                {
                    var text = pair.Left;
                    var options = pair.Right;
                    var fileOptions = options.GetOptions(text);
                    fileOptions.TryGetValue("build_metadata.AdditionalFiles.XmlEssentialsNamespace", out var ns);
                    fileOptions.TryGetValue("build_metadata.AdditionalFiles.XmlEssentialsContext", out var contextName);
                    options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                    var content = text.GetText(token)?.ToString() ?? "";
                    var namespaceName = ns ?? "";
                    if (string.IsNullOrWhiteSpace(namespaceName))
                    {
                        namespaceName = string.IsNullOrWhiteSpace(rootNamespace)
                            ? "XmlEssentials.Generated"
                            : rootNamespace + ".Generated";
                    }

                    return new XsdFileInput(text.Path, content, namespaceName, contextName ?? "");
                });

            context.RegisterSourceOutput(files.Collect(), static (production, collected) => Emit(production, collected));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Parses collected schema files and emits POCOs plus contexts.
        /// </summary>
        /// <param name="context">The production context.</param>
        /// <param name="files">The AdditionalFiles.</param>
        internal static void Emit(SourceProductionContext context, ImmutableArray<XsdFileInput> files)
        {
            var list = new List<XsdFileInput>(files.Length);
            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file.Text))
                {
                    continue;
                }

                list.Add(file);
            }

            var diagnostics = new List<Diagnostic>();
            var (pocos, contexts) = XsdParser.Build(list, diagnostics);
            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var poco in pocos)
            {
                if (!seen.Add(poco.Hint))
                {
                    continue;
                }

                context.AddSource(poco.Hint, poco.Source);
            }

            foreach (var spec in contexts)
            {
                if (!seen.Add(spec.HintName))
                {
                    continue;
                }

                context.AddSource(spec.HintName, ContextEmitter.Emit(spec));
            }
        }

        #endregion

    }

}
