using System;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Options for <see cref="Utf8XmlWriter"/>.
    /// </summary>
    /// <remarks>
    /// A max-* of 0 means the default from the DoS table. Construct with
    /// <c>new XmlWriterOptions()</c> so indent defaults apply.
    /// </remarks>
    public struct XmlWriterOptions
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets whether to write a UTF-8 BOM at the first output.
        /// Default is <see langword="false"/>.
        /// </summary>
        public bool EmitByteOrderMark { get; set; }

        /// <summary>
        /// Gets or sets the indent character when <see cref="Indented"/> is true.
        /// Default is space when constructed with <see cref="XmlWriterOptions()"/>.
        /// </summary>
        public char IndentCharacter { get; set; } = ' ';

        /// <summary>
        /// Gets or sets the number of <see cref="IndentCharacter"/>s per depth.
        /// 0 means 2.
        /// </summary>
        public int IndentSize { get; set; }

        /// <summary>
        /// Gets or sets whether to pretty-print with newlines and indentation.
        /// </summary>
        public bool Indented { get; set; }

        /// <summary>
        /// Gets or sets the maximum attributes on one element, including xmlns*.
        /// 0 means 1000.
        /// </summary>
        public int MaxAttributeCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum element depth.
        /// 0 means 64.
        /// </summary>
        public int MaxDepth { get; set; }

        /// <summary>
        /// Gets or sets the maximum UTF-8 byte length of an element or attribute name.
        /// 0 means 1024.
        /// </summary>
        public int MaxNameByteCount { get; set; }

        /// <summary>
        /// Gets or sets the newline sequence when <see cref="Indented"/> is true.
        /// Default is <see cref="Environment.NewLine"/> when constructed with <see cref="XmlWriterOptions()"/>.
        /// </summary>
        public string? NewLine { get; set; } = Environment.NewLine;

        /// <summary>
        /// Gets or sets whether to omit <c>&lt;?xml ...?&gt;</c>.
        /// </summary>
        public bool OmitXmlDeclaration { get; set; }

        /// <summary>
        /// Gets or sets whether to skip name and well-formedness checks.
        /// </summary>
        public bool SkipValidation { get; set; }

        /// <summary>
        /// Gets or sets the standalone pseudo-attribute, or null to omit it.
        /// </summary>
        public bool? Standalone { get; set; }

        /// <summary>
        /// Gets or sets the XML version written in the declaration and used for Char checks.
        /// </summary>
        public XmlVersion Version { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates options with indent character space, indent size 0 (meaning 2),
        /// and <see cref="NewLine"/> equal to <see cref="Environment.NewLine"/>.
        /// </summary>
        public XmlWriterOptions()
        {
        }

        #endregion

    }

}
