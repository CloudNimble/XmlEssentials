using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Checks every piece of source the generators emit for the one thing generated code must never do: use a
    /// member that needs reflection.
    /// </summary>
    /// <remarks>
    /// Generated code is what makes a program work when it is trimmed or compiled ahead of time, so nothing in it
    /// may name a member marked <see cref="RequiresUnreferencedCodeAttribute"/> or
    /// <see cref="RequiresDynamicCodeAttribute"/>, or a member of a type so marked. The check binds each name in the
    /// generated source and looks at what it is bound to, so it does not depend on how the source is spelled.
    /// </remarks>
    internal static class GeneratedCodeChecks
    {

        #region Public Methods

        /// <summary>
        /// Returns each use, in generated source, of a member that needs reflection.
        /// </summary>
        /// <param name="compilation">The compilation that holds the generated source.</param>
        /// <param name="generated">The generated syntax trees.</param>
        /// <returns>One line for each use: the file, the text of the use, and the member it is bound to. Empty when there is none.</returns>
        internal static List<string> UsesOfReflection(Compilation compilation, ImmutableArray<SyntaxTree> generated)
        {
            var found = new List<string>();
            foreach (var tree in generated)
            {
                var model = compilation.GetSemanticModel(tree);
                foreach (var node in tree.GetRoot().DescendantNodes())
                {
                    var info = model.GetSymbolInfo(node);
                    foreach (var symbol in info.Symbol is { } bound ? [bound] : info.CandidateSymbols)
                    {
                        if (NeedsReflection(symbol) is { } attribute)
                        {
                            found.Add(System.IO.Path.GetFileName(tree.FilePath) + ": '" + node + "' is " + symbol.ToDisplayString() + ", which has " + attribute);
                        }
                    }
                }
            }

            return [.. found.Distinct()];
        }

        /// <summary>
        /// Fails when generated source uses a member that needs reflection, or turns a trimming or Native AOT warning (an <c>IL</c> code) off.
        /// </summary>
        /// <param name="compilation">The compilation that holds the generated source.</param>
        /// <param name="generated">The generated syntax trees.</param>
        internal static void MustNeedNoReflection(Compilation compilation, ImmutableArray<SyntaxTree> generated)
        {
            UsesOfReflection(compilation, generated).Should().BeEmpty("generated code must be safe to trim and to compile ahead of time");
            foreach (var tree in generated)
            {
                tree.GetRoot().DescendantTrivia()
                    .Where(static trivia => trivia.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia) && trivia.ToString().Contains("IL", System.StringComparison.Ordinal))
                    .Select(static trivia => trivia.ToString())
                    .Should().BeEmpty("generated code must not turn a trimming or Native AOT warning off");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the attribute that says <paramref name="symbol"/> needs reflection, when it or a type that holds it has one.
        /// </summary>
        /// <param name="symbol">A member or a type that generated source names.</param>
        /// <returns>The name of the attribute, or <see langword="null"/>.</returns>
        internal static string? NeedsReflection(ISymbol symbol)
        {
            // A property is used through its accessors, and each of them may carry the attribute.
            IEnumerable<ISymbol> candidates = symbol is IPropertySymbol property
                ? new ISymbol?[] { property, property.GetMethod, property.SetMethod }.OfType<ISymbol>()
                : [symbol];
            foreach (var candidate in candidates)
            {
                for (var current = candidate; current is not null; current = current.ContainingType)
                {
                    foreach (var attribute in current.GetAttributes())
                    {
                        if (attribute.AttributeClass is { Name: nameof(RequiresUnreferencedCodeAttribute) or nameof(RequiresDynamicCodeAttribute) or nameof(UnconditionalSuppressMessageAttribute) } attributeClass)
                        {
                            return attributeClass.Name;
                        }
                    }
                }
            }

            return null;
        }

        #endregion

    }

}
