using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// Incremental source generator for <c>XmlSerializerContext</c> metadata, converters, and interceptors.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class SerializerGenerator : IIncrementalGenerator
    {

        #region Public Methods

        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var contextResults = context.SyntaxProvider.ForAttributeWithMetadataName(
                SymbolHelpers.SerializableAttribute,
                static (node, _) => node is ClassDeclarationSyntax,
                static (syntaxContext, token) => TransformContext(syntaxContext, token));
            context.RegisterSourceOutput(contextResults.Collect(), static (production, results) => EmitContexts(production, results));

            var calls = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is InvocationExpressionSyntax invocation
                    && invocation.Expression is MemberAccessExpressionSyntax member
                    && member.Name.Identifier.ValueText is "Serialize" or "Deserialize",
                static (syntaxContext, token) => TransformInvocation(syntaxContext, token));
            var interceptorsEnabled = context.ParseOptionsProvider.Combine(context.AnalyzerConfigOptionsProvider)
                .Select(static (pair, _) => InterceptorsAreEnabled(pair.Left, pair.Right));
            context.RegisterSourceOutput(
                calls.Collect().Combine(contextResults.Collect()).Combine(interceptorsEnabled),
                static (production, pair) =>
                {
                    if (!pair.Right)
                    {
                        return;
                    }

                    EmitInterceptors(production, pair.Left.Left, pair.Left.Right);
                });
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns whether this compilation opted into interceptor rewrites.
        /// </summary>
        /// <param name="parseOptions">The parse options.</param>
        /// <param name="analyzerOptions">The analyzer config.</param>
        /// <returns><see langword="true"/> when interceptors should be emitted.</returns>
        internal static bool InterceptorsAreEnabled(ParseOptions parseOptions, AnalyzerConfigOptionsProvider analyzerOptions)
        {
            if (parseOptions is CSharpParseOptions csharp)
            {
                foreach (var feature in csharp.Features)
                {
                    if (feature.Key == "InterceptorsNamespaces"
                        && feature.Value.IndexOf("CloudNimble.Text.Xml.Generated", StringComparison.Ordinal) >= 0)
                    {
                        return true;
                    }
                }
            }

            analyzerOptions.GlobalOptions.TryGetValue("build_property.InterceptorsNamespaces", out var namespaces);
            return namespaces is not null && namespaces.IndexOf("CloudNimble.Text.Xml.Generated", StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// Parses a context from one [XmlSerializable] application.
        /// </summary>
        /// <param name="context">The syntax context.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The spec and diagnostics.</returns>
        internal static (ContextSpec? Spec, ImmutableArray<Diagnostic> Diagnostics) TransformContext(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            var diagnostics = new List<Diagnostic>();
            var type = ContextCollector.GetContextType(context, diagnostics, token);
            if (type is null)
            {
                return (null, diagnostics.ToImmutableArray());
            }

            var spec = ContextCollector.Build(type, diagnostics);
            return (spec, diagnostics.ToImmutableArray());
        }

        /// <summary>
        /// Emits generated contexts, skipping duplicate type symbols from multiple [XmlSerializable] attributes.
        /// </summary>
        /// <param name="context">The production context.</param>
        /// <param name="results">The transform results.</param>
        internal static void EmitContexts(SourceProductionContext context, ImmutableArray<(ContextSpec? Spec, ImmutableArray<Diagnostic> Diagnostics)> results)
        {
            var seen = new HashSet<string>();
            foreach (var result in results)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }

                if (result.Spec is not { } spec || !seen.Add(spec.ClrTypeName))
                {
                    continue;
                }

                context.AddSource(spec.HintName, ContextEmitter.Emit(spec));
            }
        }

        /// <summary>
        /// Parses an <c>XmlSerializer</c> invocation into an interceptor spec.
        /// </summary>
        /// <param name="context">The syntax context.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The spec, or null.</returns>
        internal static InterceptorSpec? TransformInvocation(GeneratorSyntaxContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (context.Node is not InvocationExpressionSyntax invocation
                || invocation.Expression is not MemberAccessExpressionSyntax member)
            {
                return null;
            }

            var symbol = context.SemanticModel.GetSymbolInfo(invocation, token).Symbol as IMethodSymbol;
            if (symbol is null)
            {
                return null;
            }

            if (symbol.ContainingType.ToDisplayString() is not "CloudNimble.Text.Xml.XmlSerializer")
            {
                return null;
            }

            if (symbol.Name is not "Serialize" and not "Deserialize")
            {
                return null;
            }

            if (symbol.Parameters.Length is 0 or > 2)
            {
                return null;
            }

            var last = symbol.Parameters[symbol.Parameters.Length - 1];
            if (last.Type.ToDisplayString() is not "CloudNimble.Text.Xml.XmlSerializerOptions?")
            {
                if (symbol.Parameters.Length != 1)
                {
                    return null;
                }
            }

            ITypeSymbol? valueType = null;
            if (symbol.Name is "Serialize" && symbol.TypeArguments.Length == 1)
            {
                valueType = symbol.TypeArguments[0];
            }
            else if (symbol.Name is "Deserialize" && symbol.TypeArguments.Length == 1)
            {
                valueType = symbol.TypeArguments[0];
            }

            if (valueType is null)
            {
                return null;
            }

            var location = context.SemanticModel.GetInterceptableLocation(invocation, token);
            if (location is null)
            {
                return null;
            }

            return new InterceptorSpec(location.Version, location.Data, symbol.Name is "Deserialize", SymbolHelpers.FullName(valueType), "", "");
        }

        /// <summary>
        /// Emits interceptor source for call sites that match a generated context type.
        /// </summary>
        /// <param name="context">The production context.</param>
        /// <param name="calls">The call sites.</param>
        /// <param name="specs">The generated contexts.</param>
        internal static void EmitInterceptors(
            SourceProductionContext context,
            ImmutableArray<InterceptorSpec?> calls,
            ImmutableArray<(ContextSpec? Spec, ImmutableArray<Diagnostic> Diagnostics)> results)
        {
            var list = new List<InterceptorSpec>();
            var specs = new List<ContextSpec>();
            foreach (var result in results)
            {
                if (result.Spec is { } spec)
                {
                    specs.Add(spec);
                }
            }

            foreach (var call in calls)
            {
                if (call is not { } candidate)
                {
                    continue;
                }

                foreach (var spec in specs)
                {
                    TypeSpec? match = null;
                    foreach (var type in spec.Types)
                    {
                        if (type.ClrTypeName == candidate.ValueTypeName)
                        {
                            match = type;
                            break;
                        }
                    }

                    if (match is not { } typeMatch)
                    {
                        continue;
                    }

                    list.Add(new InterceptorSpec(
                        candidate.Version,
                        candidate.Data,
                        candidate.IsDeserialize,
                        candidate.ValueTypeName,
                        spec.ClrTypeName,
                        typeMatch.PropertyName));
                    break;
                }
            }

            var source = InterceptorEmitter.Emit(list);
            if (source is not null)
            {
                context.AddSource("XmlSerializerInterceptors.g.cs", source);
            }
        }

        #endregion

    }

}
