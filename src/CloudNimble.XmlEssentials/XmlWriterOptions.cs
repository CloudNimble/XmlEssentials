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
        /// Gets or sets a value that specifies how the writer lays out the attributes of a start tag.
        /// </summary>
        /// <value>
        /// One of the enumeration values. The default is <see cref="XmlAttributeStyle.Inline"/>, which writes
        /// every attribute on the line of the start tag.
        /// </value>
        /// <remarks>
        /// With <see cref="XmlAttributeStyle.PerLine"/>, <see cref="AttributesPerLine"/> attributes are written
        /// to a line and each line after the first is indented to put its first attribute under the first
        /// attribute of the tag. The indentation is the white space that starts the line of the tag, and then
        /// one space for each character of the tag up to its first attribute. It applies with or without
        /// <see cref="Indented"/>.
        /// </remarks>
        public XmlAttributeStyle AttributeStyle { get; set; }

        /// <summary>
        /// Gets or sets the number of attributes the writer writes to a line when <see cref="AttributeStyle"/>
        /// is <see cref="XmlAttributeStyle.PerLine"/>.
        /// </summary>
        /// <value>
        /// The number of attributes to a line. The default is 0, which means 1. The value is not read when
        /// <see cref="AttributeStyle"/> is <see cref="XmlAttributeStyle.Inline"/>.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a negative value.
        /// </exception>
        public int AttributesPerLine
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the writer writes a UTF-8 byte order mark before its first output.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the writer writes a UTF-8 byte order mark; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        public bool EmitByteOrderMark { get; set; }

        /// <summary>
        /// Gets or sets a value that specifies how the writer writes an element that has no content.
        /// </summary>
        /// <value>
        /// One of the enumeration values. The default is <see cref="XmlEmptyElementStyle.SelfClosing"/>, which
        /// writes <c>&lt;e/&gt;</c>.
        /// </value>
        /// <remarks>
        /// The setting applies to an element ended with <see cref="Utf8XmlWriter.WriteEndElement"/> before any
        /// content was written, and to the <c>WriteEmptyElement</c> methods.
        /// <see cref="Utf8XmlWriter.WriteFullEndElement"/> always writes an end tag.
        /// </remarks>
        public XmlEmptyElementStyle EmptyElementStyle { get; set; }

        /// <summary>
        /// Gets or sets the character used to indent when <see cref="Indented"/> is <see langword="true"/>.
        /// </summary>
        /// <value>
        /// The indent character. The default is a space. The null character (<c>'\0'</c>), which is the value in
        /// <c>default(XmlWriterOptions)</c>, also means a space.
        /// </value>
        public char IndentCharacter
        {
            get;
            set
            {
                XmlLimits.ThrowIfNotIndentCharacter(value);
                field = value;
            }
        } = ' ';

        /// <summary>
        /// Gets or sets the number of <see cref="IndentCharacter"/> characters written for each level of depth
        /// when <see cref="Indented"/> is <see langword="true"/>.
        /// </summary>
        /// <value>
        /// The number of indent characters for each level. The default is 0, which means 2.
        /// </value>
        public int IndentSize
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

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
        public int MaxAttributeCount
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum depth of nested elements.
        /// </summary>
        /// <value>
        /// The maximum element depth. The default is 0, which means 64.
        /// </value>
        public int MaxDepth
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum length of an element name, an attribute name, or a prefix, in UTF-8 bytes.
        /// </summary>
        /// <value>
        /// The maximum name length, in UTF-8 bytes. The default is 0, which means 1024.
        /// </value>
        public int MaxNameByteCount
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the line ending the writer writes.
        /// </summary>
        /// <value>
        /// The line ending: <c>"\n"</c> or <c>"\r\n"</c>. The default is <see cref="Environment.NewLine"/>, in
        /// an instance created with <see langword="new"/> and in <see langword="default"/> alike.
        /// </value>
        /// <exception cref="ArgumentNullException">
        /// The property is set to <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The property is set to a value that is neither <c>"\n"</c> nor <c>"\r\n"</c>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The value is written for every line break: the ones <see cref="Indented"/> and
        /// <see cref="XmlAttributeStyle.PerLine"/> add, and the ones in text, CDATA sections, comments and
        /// processing instructions. So a document has one kind of line ending, whatever it was read with.
        /// </para>
        /// <para>
        /// No value changes. A reader gives a carriage return and a line feed back as a line feed (XML 1.0 and
        /// 1.1, section 2.11). A carriage return that is part of a value is written as a character reference,
        /// as it always is, and a line break in an attribute value is written as a character reference too.
        /// </para>
        /// </remarks>
        public string NewLine
        {
            get => field ?? Environment.NewLine;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                XmlLimits.ThrowIfNotNewLine(value);
                field = value;
            }
        }

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
