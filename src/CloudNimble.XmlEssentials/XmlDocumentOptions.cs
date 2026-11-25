using System;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Provides the ability for the user to define custom behavior when parsing XML to create an
    /// <see cref="XmlDocument"/>.
    /// </summary>
    /// <remarks>
    /// The defaults are chosen so that a document that is parsed and then written with
    /// <see cref="XmlDocument.WriteTo(Utf8XmlWriter)"/> keeps its comments and the white space between its
    /// elements. If you don't want comments in the document, set <see cref="CommentHandling"/> to
    /// <see cref="XmlCommentHandling.Skip"/>. If you don't want white-space-only text in the document, set
    /// <see cref="WhitespaceHandling"/> to <see cref="XmlWhitespaceHandling.Insignificant"/>.
    /// <see langword="default"/> and <c>new XmlDocumentOptions()</c> are the same options.
    /// A limit property that is set to 0 uses its default limit, except
    /// <see cref="MaxDocumentByteCount"/>, where 0 means that the size is not limited.
    /// </remarks>
    public struct XmlDocumentOptions
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value that determines how the parser handles comments.
        /// </summary>
        /// <value>
        /// One of the enumeration values that indicates how comments are handled. The default is
        /// <see cref="XmlCommentHandling.Allow"/>.
        /// </value>
        /// <remarks>
        /// Comments are kept by default so that parsing a document and writing it back does not lose them.
        /// Set this property to <see cref="XmlCommentHandling.Skip"/> to leave comments out of the document,
        /// or to <see cref="XmlCommentHandling.Disallow"/> to reject a document that contains one.
        /// </remarks>
        public XmlCommentHandling CommentHandling { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of attributes allowed on one element.
        /// </summary>
        /// <value>
        /// The maximum number of attributes on one element, including namespace declarations
        /// (<c>xmlns</c> and <c>xmlns:prefix</c>). The default is 0, which means 1000.
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
        /// Gets or sets the maximum size allowed for the content of one comment.
        /// </summary>
        /// <value>
        /// The maximum size of the content of a comment, in UTF-8 bytes. The default is 0, which means 1,048,576.
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
        /// Gets or sets the maximum depth allowed when parsing XML.
        /// </summary>
        /// <value>
        /// The maximum depth of nested elements. The default is 0, which means 64.
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
        /// Gets or sets the maximum size allowed for the document.
        /// </summary>
        /// <value>
        /// The maximum size of the document, in UTF-8 bytes. The default is 0, which means that the size is not limited.
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
        /// Gets or sets the maximum size allowed for an element name or an attribute name.
        /// </summary>
        /// <value>
        /// The maximum size of an element or attribute name, in UTF-8 bytes. The default is 0, which means 1024.
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
        /// Gets or sets the maximum size allowed for a text node, a CDATA section, an attribute value, or the data of a processing instruction.
        /// </summary>
        /// <value>
        /// The maximum size of each, in UTF-8 bytes as they appear in the document. The default is 0, which means
        /// 16,777,216.
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
        /// Gets or sets a value that determines how the parser handles text between elements that consists only of white space.
        /// </summary>
        /// <value>
        /// One of the enumeration values that indicates how white-space-only text between elements is handled.
        /// The default is <see cref="XmlWhitespaceHandling.Significant"/>.
        /// </value>
        /// <remarks>
        /// White space between elements, such as indentation, is kept by default so that parsing a document
        /// and writing it back does not change its layout. Each run of white space is a text node, so it
        /// appears when you enumerate an element's children. Set this property to
        /// <see cref="XmlWhitespaceHandling.Insignificant"/> to leave white-space-only text out of the
        /// document, or to <see cref="XmlWhitespaceHandling.None"/> to reject a document that contains it.
        /// </remarks>
        public XmlWhitespaceHandling WhitespaceHandling { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlDocumentOptions"/> struct.
        /// </summary>
        /// <remarks>
        /// <see cref="CommentHandling"/> is <see cref="XmlCommentHandling.Allow"/>,
        /// <see cref="WhitespaceHandling"/> is <see cref="XmlWhitespaceHandling.Significant"/>, and every
        /// limit property is set to 0, which selects its default limit.
        /// </remarks>
        public XmlDocumentOptions()
        {
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Copies these options onto a reader options struct.
        /// </summary>
        /// <returns>Reader options with the same limits and comment/whitespace handling.</returns>
        internal readonly XmlReaderOptions ToReaderOptions()
        {
            return new XmlReaderOptions
            {
                CommentHandling = CommentHandling,
                MaxAttributeCount = MaxAttributeCount,
                MaxCommentByteCount = MaxCommentByteCount,
                MaxDepth = MaxDepth,
                MaxDocumentByteCount = MaxDocumentByteCount,
                MaxNameByteCount = MaxNameByteCount,
                MaxTextByteCount = MaxTextByteCount,
                WhitespaceHandling = WhitespaceHandling,
            };
        }

        #endregion

    }

}
