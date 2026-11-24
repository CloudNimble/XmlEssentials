using System;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Provides options that control how a <see cref="Utf8XmlReader"/> reads XML.
    /// </summary>
    /// <remarks>
    /// A limit property set to 0 uses the default limit stated on that property, except
    /// <see cref="MaxDocumentByteCount"/>, where 0 means no limit.
    /// <see langword="default"/> and <c>new XmlReaderOptions()</c> are the same options. By default, the reader
    /// reports comments and whitespace-only text between elements, so that XML that is read and written back
    /// keeps its comments and its layout. For a read loop that wants data only, set
    /// <see cref="CommentHandling"/> to <see cref="XmlCommentHandling.Skip"/> and
    /// <see cref="WhitespaceHandling"/> to <see cref="XmlWhitespaceHandling.Insignificant"/>.
    /// </remarks>
    public struct XmlReaderOptions
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value that indicates whether the reader accepts a document fragment, which can
        /// have more than one top-level element or none.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if more than one top-level element, or none, is allowed; otherwise,
        /// <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool AllowMultipleRoots { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the encoding named in the XML declaration is left unchecked.
        /// </summary>
        /// <value>
        /// <see langword="true"/> when the reader was given UTF-8 that this library transcoded from characters,
        /// so the declaration describes a copy of the document that no longer exists; otherwise,
        /// <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// <c>System.Xml</c> writes <c>encoding="utf-16"</c> into a document it writes to a string, and reads it
        /// back without complaint. A caller of the reader passes bytes, and cannot set this.
        /// </remarks>
        internal bool IgnoreDeclaredEncoding { get; set; }

        /// <summary>
        /// Gets or sets a value that defines how the reader handles comments.
        /// </summary>
        /// <value>
        /// One of the enumeration values that indicates how comments are handled. The default is
        /// <see cref="XmlCommentHandling.Allow"/>.
        /// </value>
        /// <remarks>
        /// Comments are reported by default so that XML that is read and written back does not lose them.
        /// Set this property to <see cref="XmlCommentHandling.Skip"/> when your code does not want to
        /// see <see cref="XmlTokenType.Comment"/> tokens.
        /// </remarks>
        public XmlCommentHandling CommentHandling { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of attributes allowed on one element, including namespace
        /// declarations.
        /// </summary>
        /// <value>
        /// The maximum number of attributes on one element. The default is 0, which means a maximum of 1000.
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
        /// Gets or sets the maximum size of the content of a comment.
        /// </summary>
        /// <value>
        /// The maximum size of a comment's content, in UTF-8 bytes. The default is 0, which means a maximum
        /// of 1,048,576.
        /// </value>
        public int MaxCommentByteCount
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum depth of nested elements allowed when reading XML.
        /// </summary>
        /// <value>
        /// The maximum element depth. The default is 0, which means a maximum depth of 64.
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
        /// Gets or sets the maximum size of the document.
        /// </summary>
        /// <value>
        /// The maximum size of the document, in bytes. The default is 0, which means no limit.
        /// </value>
        public int MaxDocumentByteCount
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum length of an element or attribute name.
        /// </summary>
        /// <value>
        /// The maximum length of a name, in UTF-8 bytes. The default is 0, which means a maximum of 1024.
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
        /// Gets or sets the maximum length of a text or CDATA node.
        /// </summary>
        /// <value>
        /// The maximum length of a text or CDATA node, in UTF-8 bytes. The default is 0, which means a
        /// maximum of 16,777,216.
        /// </value>
        public int MaxTextByteCount
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that defines how the reader handles whitespace-only text between elements.
        /// </summary>
        /// <value>
        /// One of the enumeration values that indicates how whitespace-only text is handled. The default is
        /// <see cref="XmlWhitespaceHandling.Significant"/>.
        /// </value>
        /// <remarks>
        /// Whitespace-only text between elements is reported by default so that XML that is read and written
        /// back does not change its layout. Set this property to <see cref="XmlWhitespaceHandling.Insignificant"/>
        /// when your code does not want to see those <see cref="XmlTokenType.Text"/> tokens.
        /// </remarks>
        public XmlWhitespaceHandling WhitespaceHandling { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlReaderOptions"/> struct.
        /// </summary>
        /// <remarks>
        /// The result is the same as <see langword="default"/>: <see cref="CommentHandling"/> is
        /// <see cref="XmlCommentHandling.Allow"/>, <see cref="WhitespaceHandling"/> is
        /// <see cref="XmlWhitespaceHandling.Significant"/>, and every limit property is 0, which selects
        /// its default limit.
        /// </remarks>
        public XmlReaderOptions()
        {
        }

        #endregion

    }

}
