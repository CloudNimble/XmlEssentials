using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// An in-memory AdditionalFile for generator tests.
    /// </summary>
    internal sealed class TestAdditionalText : AdditionalText
    {

        #region Fields

        private readonly SourceText _text;

        #endregion

        #region Public Properties

        /// <inheritdoc />
        public override string Path { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the additional file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="text">The file contents.</param>
        internal TestAdditionalText(string path, string text)
        {
            Path = path;
            _text = SourceText.From(text);
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return _text;
        }

        #endregion

    }

}
