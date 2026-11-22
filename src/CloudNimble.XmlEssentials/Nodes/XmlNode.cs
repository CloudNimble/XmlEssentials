using System;
using System.Buffers;
using System.Text;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// A mutable XML node, analogous to <c>JsonNode</c> but as a real XML tree.
    /// </summary>
    public abstract class XmlNode
    {

        #region Fields

        private XmlNode? _parent;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the namespace URI, or <see langword="null"/> when the node has none.
        /// </summary>
        public virtual string? NamespaceUri
        {
            get => null;
            set { }
        }

        /// <summary>
        /// Gets the parent node, or <see langword="null"/> when this node is unattached.
        /// </summary>
        public XmlNode? Parent => _parent;

        /// <summary>
        /// Gets the node kind.
        /// </summary>
        public abstract XmlValueKind ValueKind { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a deep copy of this node, unattached from any parent.
        /// </summary>
        /// <returns>The clone.</returns>
        public abstract XmlNode DeepClone();

        /// <summary>
        /// Parses <paramref name="xml"/> into a mutable tree whose root is the document element.
        /// </summary>
        /// <param name="xml">The XML text.</param>
        /// <returns>The root element node, or <see langword="null"/> when the document is empty.</returns>
        public static XmlNode? Parse(string xml)
        {
            ArgumentNullException.ThrowIfNull(xml);
            using var document = XmlDocument.Parse(xml, new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Allow });
            return document.RootElement.CloneNode();
        }

        /// <summary>
        /// Parses UTF-8 XML into a mutable tree whose root is the document element.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 XML.</param>
        /// <returns>The root element node.</returns>
        public static XmlNode? Parse(ReadOnlySpan<byte> utf8Xml)
        {
            using var document = XmlDocument.Parse(utf8Xml, new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Allow });
            return document.RootElement.CloneNode();
        }

        /// <summary>
        /// Parses the current element from <paramref name="reader"/> into a mutable tree.
        /// </summary>
        /// <param name="reader">A reader positioned at or before a start tag.</param>
        /// <returns>The element node.</returns>
        public static XmlNode? ParseValue(ref Utf8XmlReader reader)
        {
            using var document = XmlDocument.ParseValue(ref reader);
            return document.RootElement.CloneNode();
        }

        /// <summary>
        /// Writes this node to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The destination writer.</param>
        public abstract void WriteTo(Utf8XmlWriter writer);

        /// <summary>
        /// Returns the XML markup for this node.
        /// </summary>
        /// <returns>The markup.</returns>
        public override string ToString()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            WriteTo(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(buffer.WrittenSpan);
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
