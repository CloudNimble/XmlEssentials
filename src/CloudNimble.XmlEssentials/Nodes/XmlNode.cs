using System;
using System.Buffers;
using System.Text;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// Defines the core behavior of a node in a mutable XML tree and provides a base for the classes that
    /// represent elements, attributes, text, CDATA sections, comments, processing instructions, and the
    /// XML declaration.
    /// </summary>
    /// <remarks>
    /// This class is the XML counterpart of <c>JsonNode</c> in System.Text.Json. Unlike
    /// <see cref="XmlDocument"/>, the tree can be changed, uses no pooled memory, and does not need to be disposed.
    /// </remarks>
    public abstract class XmlNode
    {

        #region Fields

        private XmlNode? _parent;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the namespace URI of this node.
        /// </summary>
        /// <value>
        /// The namespace URI of the node. An element or an attribute in no namespace gives an empty string.
        /// For a node that is not an element or an attribute, the value is always <see langword="null"/>,
        /// and setting it has no effect.
        /// </value>
        public virtual string? NamespaceUri
        {
            get => null;
            set { }
        }

        /// <summary>
        /// Gets the parent of this node.
        /// </summary>
        /// <value>
        /// The element that holds this node as a child or as an attribute, or <see langword="null"/> if this
        /// node has no parent.
        /// </value>
        public XmlNode? Parent => _parent;

        /// <summary>
        /// When overridden in a derived class, gets the kind of this node.
        /// </summary>
        /// <value>
        /// One of the enumeration values that indicates the kind of this node.
        /// </value>
        public abstract XmlValueKind ValueKind { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// When overridden in a derived class, creates a new instance of the node. All children and
        /// attributes are recursively cloned.
        /// </summary>
        /// <returns>
        /// A new cloned instance of the current node. The clone has no parent.
        /// </returns>
        public abstract XmlNode DeepClone();

        /// <summary>
        /// Parses a string that holds a single XML document into a mutable tree.
        /// </summary>
        /// <param name="xml">The XML text to parse.</param>
        /// <returns>
        /// An <see cref="XmlNode"/> representation of the document element.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="xml"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="xml"/> is not a well-formed XML document.
        /// -or-
        /// <paramref name="xml"/> exceeds a default limit of <see cref="XmlDocumentOptions"/>.
        /// </exception>
        /// <remarks>
        /// Comments inside the document element are kept. The XML declaration and any comments or processing
        /// instructions outside the document element are not part of the returned tree.
        /// </remarks>
        public static XmlNode Parse(string xml)
        {
            ArgumentNullException.ThrowIfNull(xml);
            using var document = XmlDocument.Parse(xml, new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Allow });
            return document.RootElement.CloneNode();
        }

        /// <summary>
        /// Parses a span of UTF-8 bytes that holds a single XML document into a mutable tree.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 encoded XML text to parse.</param>
        /// <returns>
        /// An <see cref="XmlNode"/> representation of the document element.
        /// </returns>
        /// <exception cref="XmlException">
        /// <paramref name="utf8Xml"/> is not a well-formed XML document.
        /// -or-
        /// <paramref name="utf8Xml"/> exceeds a default limit of <see cref="XmlDocumentOptions"/>.
        /// </exception>
        /// <remarks>
        /// Comments inside the document element are kept. The XML declaration and any comments or processing
        /// instructions outside the document element are not part of the returned tree.
        /// </remarks>
        public static XmlNode Parse(ReadOnlySpan<byte> utf8Xml)
        {
            using var document = XmlDocument.Parse(utf8Xml, new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Allow });
            return document.RootElement.CloneNode();
        }

        /// <summary>
        /// Parses one XML element (including its attributes and descendants) from the provided reader into a mutable tree.
        /// </summary>
        /// <param name="reader">The reader to read. It is positioned on or before the start tag of the element.</param>
        /// <returns>
        /// An <see cref="XmlNode"/> representation of the element (and nested nodes) read from the reader.
        /// </returns>
        /// <exception cref="XmlException">
        /// <paramref name="reader"/> reaches an end tag before it reaches a start tag.
        /// -or-
        /// <paramref name="reader"/> reaches the end of its input without reading an element.
        /// -or-
        /// The XML that <paramref name="reader"/> reads is not well formed.
        /// </exception>
        /// <remarks>
        /// When this method returns, the reader is positioned on the end tag of the element, or on the
        /// element itself if it is an empty-element tag. Comments are kept only if the reader reports them.
        /// </remarks>
        public static XmlNode ParseValue(ref Utf8XmlReader reader)
        {
            using var document = XmlDocument.ParseValue(ref reader);
            return document.RootElement.CloneNode();
        }

        /// <summary>
        /// When overridden in a derived class, writes this node to the provided writer as XML.
        /// </summary>
        /// <param name="writer">The writer to which to write the node.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/> is <see langword="null"/>.
        /// </exception>
        public abstract void WriteTo(Utf8XmlWriter writer);

        /// <summary>
        /// Converts the current instance to a string that holds its XML markup.
        /// </summary>
        /// <returns>
        /// The XML markup of this node, including its attributes and descendants. No XML declaration is added.
        /// </returns>
        /// <exception cref="XmlException">
        /// The tree cannot be written as XML, for example because a name has a prefix but no namespace.
        /// </exception>
        public override string ToString()
        {
            var buffer = new ArrayBufferWriter<byte>();
            // A node does not know which XML version it was parsed from. XML 1.1 can carry every character
            // either version allows, and writes the ones it restricts or normalizes as references.
            var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version11 });
            try
            {
                WriteTo(writer);
                writer.Flush();
                return Encoding.UTF8.GetString(buffer.WrittenSpan);
            }
            catch
            {
                // What was written is abandoned. Otherwise disposing the writer would report the elements
                // this failure left open, in place of the failure itself.
                writer.Reset();
                throw;
            }
            finally
            {
                writer.Dispose();
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Sets the parent pointer.
        /// </summary>
        /// <param name="parent">The parent, or <see langword="null"/>.</param>
        internal void SetParent(XmlNode? parent)
        {
            _parent = parent;
        }

        #endregion

    }

}
