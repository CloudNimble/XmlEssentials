using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Analyzer config options used by generator tests.
    /// </summary>
    internal sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {

        #region Fields

        private readonly Dictionary<string, string> _values;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets empty options.
        /// </summary>
        internal static TestAnalyzerConfigOptions Empty { get; } = new(new Dictionary<string, string>());

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the options.
        /// </summary>
        /// <param name="values">The key/value pairs.</param>
        internal TestAnalyzerConfigOptions(Dictionary<string, string> values)
        {
            _values = values;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            return _values.TryGetValue(key, out value);
        }

        #endregion

    }

}
