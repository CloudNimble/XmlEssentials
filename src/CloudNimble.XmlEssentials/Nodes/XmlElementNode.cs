using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// A mutable XML element with attributes and mixed children.
    /// </summary>
    public sealed class XmlElementNode : XmlNode, IEnumerable<XmlNode>
    {

        #region Fields

        private string _localName;
        private string _namespaceUri;
        private string _prefix;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the attributes of this element.
        /// </summary>
        public XmlAttributeNodeList Attributes { get; }

        /// <summary>
        /// Gets the child nodes of this element.
        /// </summary>
        public XmlNodeList Children { get; }

        /// <summary>
        /// Gets or sets concatenated descendant text. Setting replaces all children with one text node.
        /// </summary>
        public string InnerText
        {
            get
            {
                var builder = new StringBuilder();
                AppendText(this, builder);
                return builder.ToString();
            }
            set
            {
                Children.Clear();
                if (value is { Length: > 0 })
                {
                    Children.Add(new XmlTextNode(value));
                }
            }
        }

        /// <summary>
        /// Gets or sets the local name.
        /// </summary>
        public string LocalName
        {
            get => _localName;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                _localName = value;
            }
        }

        /// <summary>
        /// Gets or sets the namespace URI.
        /// </summary>
        public override string? NamespaceUri
        {
            get => _namespaceUri;
            set => _namespaceUri = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the prefix.
        /// </summary>
        public string Prefix
        {
            get => _prefix;
            set => _prefix = value ?? string.Empty;
        }

        /// <inheritdoc />
        public override XmlValueKind ValueKind => XmlValueKind.Element;

        /// <summary>
        /// Gets the first child element with <paramref name="localName"/>, or <see langword="null"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        public XmlElementNode? this[string localName]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(localName);
                foreach (var child in Children)
                {
                    if (child is XmlElementNode element && element.LocalName == localName)
                    {
                        return element;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the first child element with <paramref name="localName"/> in <paramref name="namespaceUri"/>, or <see langword="null"/>.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        public XmlElementNode? this[string localName, string namespaceUri]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(localName);
                ArgumentNullException.ThrowIfNull(namespaceUri);
                foreach (var child in Children)
                {
                    if (child is XmlElementNode element
                        && element.LocalName == localName
                        && element.NamespaceUri == namespaceUri)
                    {
                        return element;
                    }
                }

                return null;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an element.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <param name="prefix">The prefix.</param>
        public XmlElementNode(string localName, string? namespaceUri = null, string? prefix = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localName);
            _localName = localName;
            _namespaceUri = namespaceUri ?? string.Empty;
            _prefix = prefix ?? string.Empty;
            Attributes = new XmlAttributeNodeList(this);
            Children = new XmlNodeList(this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Appends <paramref name="node"/> to the child list.
        /// </summary>
        /// <param name="node">The node.</param>
        public void Add(XmlNode node)
        {
            Children.Add(node);
        }

        /// <summary>
        /// Creates and appends a child element.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <returns>The new child.</returns>
        public XmlElementNode AddElement(string localName, string? namespaceUri = null)
        {
            var child = new XmlElementNode(localName, namespaceUri);
            Children.Add(child);
            return child;
        }

        /// <inheritdoc />
        public override XmlNode DeepClone()
        {
            var clone = new XmlElementNode(_localName, _namespaceUri, _prefix);
            foreach (var attribute in Attributes)
            {
                clone.Attributes.Add((XmlAttributeNode)attribute.DeepClone());
            }

            foreach (var child in Children)
            {
                clone.Add(child.DeepClone());
            }

            return clone;
        }

        /// <inheritdoc />
        public IEnumerator<XmlNode> GetEnumerator()
        {
            return Children.GetEnumerator();
        }

        /// <summary>
        /// Sets or replaces an attribute.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <returns>The attribute node.</returns>
        public XmlAttributeNode SetAttribute(string localName, string? value, string? namespaceUri = null)
        {
            var existing = namespaceUri is null
                ? Attributes[localName]
                : Attributes[localName, namespaceUri];
            if (existing is not null)
            {
                existing.Value = value ?? string.Empty;
                return existing;
            }

            var attribute = new XmlAttributeNode(localName, value, namespaceUri);
            Attributes.Add(attribute);
            return attribute;
        }

        /// <inheritdoc />
        public override void WriteTo(Utf8XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            var prefix = Utf8XmlWriter.EncodeUtf8(_prefix, stackalloc byte[64], out _);
            var ns = Utf8XmlWriter.EncodeUtf8(_namespaceUri, stackalloc byte[128], out _);
            writer.WriteStartElementDeferred(Utf8XmlWriter.EncodeUtf8(_localName, stackalloc byte[64], out _), prefix);
            var declared = false;
            Span<byte> prefixScratch = stackalloc byte[64];
            Span<byte> uriScratch = stackalloc byte[128];
            foreach (var attribute in Attributes)
            {
                if (!IsNamespaceDeclaration(attribute, out var declaredPrefix))
                {
                    continue;
                }

                // The element's own name is authoritative: if this declaration is for its prefix,
                // write the element's namespace even when the attribute node holds a stale value.
                var isOwn = declaredPrefix == _prefix;
                declared |= isOwn;
                writer.RegisterNamespace(
                    Utf8XmlWriter.EncodeUtf8(declaredPrefix, prefixScratch, out _),
                    isOwn ? ns : Utf8XmlWriter.EncodeUtf8(attribute.Value, uriScratch, out _));
            }

            if (!declared)
            {
                writer.EnsureElementNamespace(prefix, ns);
            }

            foreach (var attribute in Attributes)
            {
                if (!IsNamespaceDeclaration(attribute, out var declaredPrefix))
                {
                    attribute.WriteTo(writer);
                    continue;
                }

                writer.WriteRegisteredNamespace(
                    Utf8XmlWriter.EncodeUtf8(declaredPrefix, prefixScratch, out _),
                    declaredPrefix == _prefix ? ns : Utf8XmlWriter.EncodeUtf8(attribute.Value, uriScratch, out _));
            }

            foreach (var child in Children)
            {
                child.WriteTo(writer);
            }

            writer.WriteEndElement();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns whether <paramref name="attribute"/> is an <c>xmlns</c> or <c>xmlns:prefix</c> declaration.
        /// </summary>
        /// <param name="attribute">The attribute node.</param>
        /// <param name="declaredPrefix">When this method returns true, the declared prefix, or empty for the default namespace.</param>
        /// <returns><see langword="true"/> for a namespace declaration. An attribute such as <c>p:xmlns</c> is not one.</returns>
        internal static bool IsNamespaceDeclaration(XmlAttributeNode attribute, out string declaredPrefix)
        {
            if (attribute.Prefix.Length == 0)
            {
                declaredPrefix = string.Empty;
                return attribute.LocalName == "xmlns";
            }

            declaredPrefix = attribute.LocalName;
            return attribute.Prefix == "xmlns";
        }

        /// <summary>
        /// Appends descendant text of <paramref name="node"/> onto <paramref name="builder"/>.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="builder">The destination.</param>
        internal static void AppendText(XmlNode node, StringBuilder builder)
        {
            if (node is XmlTextNode text)
            {
                builder.Append(text.Value);
                return;
            }

            if (node is XmlCDataNode cdata)
            {
                builder.Append(cdata.Value);
                return;
            }

            if (node is XmlElementNode element)
            {
                foreach (var child in element.Children)
                {
                    AppendText(child, builder);
                }
            }
        }

        #endregion

    }

}
