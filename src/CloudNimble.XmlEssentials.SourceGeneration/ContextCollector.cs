using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// Builds <see cref="ContextSpec"/> values from [XmlSerializable] attributes.
    /// </summary>
    internal static class ContextCollector
    {

        #region Public Methods

        /// <summary>
        /// Parses a context type from one [XmlSerializable] syntax node. Returns null when the node is not a class.
        /// Diagnostics for a non-partial context are added to <paramref name="diagnostics"/>.
        /// </summary>
        /// <param name="context">The syntax context.</param>
        /// <param name="diagnostics">Diagnostics to report.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The containing type symbol, or null.</returns>
        internal static INamedTypeSymbol? GetContextType(GeneratorAttributeSyntaxContext context, List<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (context.TargetSymbol is not INamedTypeSymbol type)
            {
                return null;
            }

            var syntax = context.TargetNode as ClassDeclarationSyntax;
            var isPartial = syntax is not null && syntax.Modifiers.Any(SyntaxKind.PartialKeyword);
            if (!isPartial || !SymbolHelpers.Inherits(type, SymbolHelpers.ContextType))
            {
                diagnostics.Add(Diagnostic.Create(XmlDiagnosticDescriptors.ContextMustBePartial, type.Locations[0], type.ToDisplayString()));
                return null;
            }

            return type;
        }

        /// <summary>
        /// Builds a <see cref="ContextSpec"/> for <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The context type.</param>
        /// <param name="diagnostics">Diagnostics to report.</param>
        /// <returns>The spec.</returns>
        internal static ContextSpec Build(INamedTypeSymbol type, List<Diagnostic> diagnostics)
        {
            var optionsAttr = SymbolHelpers.GetAttribute(type, SymbolHelpers.SourceGenerationOptionsAttribute);
            var options = ReadOptions(optionsAttr);
            var generationMode = optionsAttr is null ? 0 : SymbolHelpers.GetInt(optionsAttr, "GenerationMode", 0);
            var emitSerialization = generationMode is 0 or 2 or 3;
            var roots = new List<(INamedTypeSymbol Type, string PropertyName)>();
            var names = new HashSet<string>();
            foreach (var attribute in type.GetAttributes())
            {
                if (!SymbolHelpers.Is(attribute, SymbolHelpers.SerializableAttribute))
                {
                    continue;
                }

                var target = SymbolHelpers.GetConstructorType(attribute, 0) as INamedTypeSymbol;
                if (target is null)
                {
                    continue;
                }

                var propertyName = SymbolHelpers.GetString(attribute, "TypeInfoPropertyName");
                if (string.IsNullOrEmpty(propertyName))
                {
                    propertyName = target.Name;
                }

                if (!names.Add(propertyName!))
                {
                    propertyName += names.Count.ToString();
                }

                var mode = SymbolHelpers.GetInt(attribute, "GenerationMode", 0);
                roots.Add((target, propertyName!));
                _ = mode;
            }

            var types = TypeGraphWalker.Walk(roots, options, emitSerialization, diagnostics);
            var accessibility = type.DeclaredAccessibility is Accessibility.Public ? "public" : "internal";
            var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? "" : type.ContainingNamespace.ToDisplayString();
            return new ContextSpec(
                SymbolHelpers.FullName(type),
                type.Name,
                namespaceName,
                accessibility,
                generationMode,
                options,
                types);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reads <see cref="OptionsSpec"/> from <paramref name="attribute"/>.
        /// </summary>
        /// <param name="attribute">The attribute, or null.</param>
        /// <returns>The options.</returns>
        internal static OptionsSpec ReadOptions(AttributeData? attribute)
        {
            if (attribute is null)
            {
                return new OptionsSpec(false, false, false, "", 0, 0, 0, 0, 0, false, 0);
            }

            return new OptionsSpec(
                true,
                SymbolHelpers.GetBool(attribute, "WriteIndented"),
                SymbolHelpers.GetBool(attribute, "OmitXmlDeclaration"),
                SymbolHelpers.GetString(attribute, "DefaultNamespace") ?? "",
                SymbolHelpers.GetInt(attribute, "PropertyNamingPolicy", 0),
                SymbolHelpers.GetInt(attribute, "ElementNamingPolicy", 0),
                SymbolHelpers.GetInt(attribute, "AttributeNamingPolicy", 0),
                SymbolHelpers.GetInt(attribute, "DefaultIgnoreCondition", 0),
                SymbolHelpers.GetInt(attribute, "DefaultPropertyMapping", 0),
                SymbolHelpers.GetBool(attribute, "IncludeFields"),
                SymbolHelpers.GetInt(attribute, "NilHandling", 0));
        }

        #endregion

    }

}
