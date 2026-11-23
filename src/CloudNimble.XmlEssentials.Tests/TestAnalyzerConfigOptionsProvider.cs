using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Supplies analyzer config options for generator tests, including AdditionalFile metadata.
    /// </summary>
    internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {

        #region Fields

        private readonly Dictionary<string, AnalyzerConfigOptions> _byPath;
        private readonly AnalyzerConfigOptions _global;

        #endregion

        #region Public Properties

        /// <inheritdoc />
        public override AnalyzerConfigOptions GlobalOptions => _global;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the provider.
        /// </summary>
        /// <param name="global">The global options.</param>
        /// <param name="byPath">Per-AdditionalFile options keyed by path.</param>
        internal TestAnalyzerConfigOptionsProvider(AnalyzerConfigOptions global, Dictionary<string, AnalyzerConfigOptions> byPath)
        {
            _global = global;
            _byPath = byPath;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return _byPath.TryGetValue(textFile.Path, out var options) ? options : TestAnalyzerConfigOptions.Empty;
        }

        /// <inheritdoc />
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return TestAnalyzerConfigOptions.Empty;
        }

        #endregion

    }

}
