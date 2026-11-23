using System;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Provides the options that control how a <see cref="Utf8XmlWriter"/> formats and checks the XML it writes.
    /// </summary>
    /// <remarks>
    /// A limit property (<see cref="MaxAttributeCount"/>, <see cref="MaxDepth"/>, <see cref="MaxNameByteCount"/>)
    /// that is 0 uses the default limit that the property documents. <c>new XmlWriterOptions()</c> sets
    /// <see cref="IndentCharacter"/> to a space and <see cref="NewLine"/> to <see cref="Environment.NewLine"/>.
    /// <c>default(XmlWriterOptions)</c> leaves them unset, and the writer then uses the same two values.
    /// </remarks>
    public struct XmlWriterOptions
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value that indicates whether the writer writes a UTF-8 byte order mark before its first output.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the writer writes a UTF-8 byte order mark; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        public bool EmitByteOrderMark { get; set; }

        /// <summary>
        /// Gets or sets the character used to indent when <see cref="Indented"/> is <see langword="true"/>.
        /// </summary>
        /// <value>
        /// The indent character. The default is a space. The null character (<c>'\0'</c>), which is the value in
        /// <c>default(XmlWriterOptions)</c>, also means a space.
        /// </value>
        public char IndentCharacter { get; set; } = ' ';

        /// <summary>
        /// Gets or sets the number of <see cref="IndentCharacter"/> characters written for each level of depth
        /// when <see cref="Indented"/> is <see langword="true"/>.
        /// </summary>
        /// <value>
        /// The number of indent characters for each level. The default is 0, which means 2.
        /// </value>
        public int IndentSize { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the writer formats the output with new lines and indentation.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the writer puts each element on its own line and indents it; otherwise,
        /// <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool Indented { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of attributes on one element, including namespace declarations.
        /// </summary>
        /// <value>
        /// The maximum number of attributes on one element. The default is 0, which means 1000.
        /// </value>
        public int MaxAttributeCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum depth of nested elements.
        /// </summary>
        /// <value>
        /// The maximum element depth. The default is 0, which means 64.
        /// </value>
        public int MaxDepth { get; set; }

        /// <summary>
        /// Gets or sets the maximum length of an element name, an attribute name, or a prefix, in UTF-8 bytes.
        /// </summary>
        /// <value>
        /// The maximum name length, in UTF-8 bytes. The default is 0, which means 1024.
        /// </value>
        public int MaxNameByteCount { get; set; }

        /// <summary>
        /// Gets or sets the line ending written before each indented line when <see cref="Indented"/> is <see langword="true"/>.
        /// </summary>
        /// <value>
        /// The line ending. The default is <see cref="Environment.NewLine"/>. A <see langword="null"/> or empty
        /// value also means <see cref="Environment.NewLine"/>.
        /// </value>
        public string? NewLine { get; set; } = Environment.NewLine;

        /// <summary>
        /// Gets or sets a value that indicates whether the writer omits the XML declaration (<c>&lt;?xml ...?&gt;</c>).
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the writer does not write the XML declaration on its own; otherwise,
        /// <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// <see cref="Utf8XmlWriter.WriteXmlDeclaration"/> writes the declaration whatever this value is.
        /// </remarks>
        public bool OmitXmlDeclaration { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the writer skips its name, namespace, and duplicate attribute checks.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the writer skips those checks; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// When the value is <see langword="true"/>, the writer does not check that names are valid XML names or
        /// fit in <see cref="MaxNameByteCount"/>, that prefixes and namespace declarations follow Namespaces in XML,
        /// that an element has no duplicate attribute, that the characters of a comment, CDATA section, or
        /// processing instruction are allowed, or that every element is closed when the writer is disposed.
        /// The writer still enforces a single root element, <see cref="MaxDepth"/>, and <see cref="MaxAttributeCount"/>.
        /// </remarks>
        public bool SkipValidation { get; set; }

        /// <summary>
        /// Gets or sets the value of the <c>standalone</c> pseudo-attribute of the XML declaration.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to write <c>standalone="yes"</c>; <see langword="false"/> to write
        /// <c>standalone="no"</c>; <see langword="null"/> to omit the pseudo-attribute. The default is <see langword="null"/>.
        /// </value>
        public bool? Standalone { get; set; }

        /// <summary>
        /// Gets or sets the XML version that the writer writes in the XML declaration and uses to decide which
        /// characters are allowed.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies the XML version. The default is <see cref="XmlVersion.Version10"/>.
        /// </value>
        public XmlVersion Version { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlWriterOptions"/> struct with a space as the indent
        /// character, an indent size of 0 (which means 2), and <see cref="Environment.NewLine"/> as the line ending.
        /// </summary>
        public XmlWriterOptions()
        {
        }

        #endregion

    }

}
