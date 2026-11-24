using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// Represents a mutable XML element with attributes and child nodes. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// The children can be elements, text nodes, CDATA sections, comments, and processing instructions, in any order.
    /// Enumerating the element enumerates <see cref="Children"/>.
    /// </remarks>
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
        /// <value>
        /// The list of attributes of this element, in the order they are written. The list can be changed.
        /// </value>
        public XmlAttributeNodeList Attributes { get; }

        /// <summary>
        /// Gets the child nodes of this element.
        /// </summary>
        /// <value>
        /// The list of child nodes of this element, in document order. The list can be changed.
        /// </value>
        public XmlNodeList Children { get; }

        /// <summary>
        /// Gets or sets the text content of this element.
        /// </summary>
        /// <value>
        /// The text of all text nodes and CDATA sections inside this element, in document order. Setting
        /// the value removes all children and adds one text node that holds the value. Setting
        /// <see langword="null"/> or an empty string removes all children and adds nothing.
        /// </value>
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
        /// Gets or sets the local name of this element.
        /// </summary>
        /// <value>
        /// The local name of the element, without a prefix.
        /// </value>
        /// <exception cref="ArgumentNullException">
        /// The value in a set operation is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The value in a set operation is empty or consists only of white-space characters.
        /// </exception>
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
        /// Gets or sets the namespace URI of this element.
        /// </summary>
        /// <value>
        /// The namespace URI of the element, or an empty string if the element is in no namespace. The
        /// value is never <see langword="null"/>. Setting <see langword="null"/> stores an empty string.
        /// </value>
        public override string? NamespaceUri
        {
            get => _namespaceUri;
            set => _namespaceUri = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the namespace prefix of this element.
        /// </summary>
        /// <value>
        /// The prefix of the element name, or an empty string if the name has no prefix. Setting
        /// <see langword="null"/> stores an empty string.
        /// </value>
        public string Prefix
        {
            get => _prefix;
            set => _prefix = value ?? string.Empty;
        }

        /// <inheritdoc />
        public override XmlValueKind ValueKind => XmlValueKind.Element;

        /// <summary>
        /// Gets the first child element that has the specified local name, in any namespace.
        /// </summary>
        /// <param name="localName">The local name of the child element to find, without a prefix.</param>
        /// <value>
        /// The first child element that has the specified local name, or <see langword="null"/> if there is none.
        /// </value>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="localName"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// Only direct children are searched, not deeper descendants.
        /// </remarks>
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
        /// Gets the first child element that has the specified local name and namespace URI.
        /// </summary>
        /// <param name="localName">The local name of the child element to find, without a prefix.</param>
        /// <param name="namespaceUri">The namespace URI of the child element, or an empty string for an element in no namespace.</param>
        /// <value>
        /// The first child element that has the specified local name and namespace URI, or
        /// <see langword="null"/> if there is none.
        /// </value>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="localName"/> or <paramref name="namespaceUri"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// Only direct children are searched, not deeper descendants.
        /// </remarks>
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
        /// Initializes a new instance of the <see cref="XmlElementNode"/> class with the specified local name, namespace URI, and prefix.
        /// </summary>
        /// <param name="localName">The local name of the element, without a prefix.</param>
        /// <param name="namespaceUri">The namespace URI of the element, or <see langword="null"/> or an empty string for an element in no namespace. The default is <see langword="null"/>.</param>
        /// <param name="prefix">The prefix of the element name, or <see langword="null"/> or an empty string for no prefix. The default is <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="localName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="localName"/> is empty or consists only of white-space characters.
        /// </exception>
        /// <remarks>
        /// The new element has no attributes, no children, and no parent.
        /// </remarks>
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
        /// Adds the specified node to the end of the child nodes of this element.
        /// </summary>
        /// <param name="node">The node to add. It must not have a parent.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="node"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="node"/> already has a parent.
        /// </exception>
        public void Add(XmlNode node)
        {
            Children.Add(node);
        }

        /// <summary>
        /// Creates an element with the specified local name and namespace URI and adds it to the end of the child nodes of this element.
        /// </summary>
        /// <param name="localName">The local name of the new element, without a prefix.</param>
        /// <param name="namespaceUri">The namespace URI of the new element, or <see langword="null"/> or an empty string for an element in no namespace. The default is <see langword="null"/>.</param>
        /// <returns>
        /// The new child element. It has no prefix.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="localName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="localName"/> is empty or consists only of white-space characters.
        /// </exception>
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
        /// Sets the value of the attribute that has the specified name, or adds the attribute if this element does not have it.
        /// </summary>
        /// <param name="localName">The local name of the attribute, without a prefix.</param>
        /// <param name="value">The value of the attribute. <see langword="null"/> is stored as an empty string.</param>
        /// <param name="namespaceUri">The namespace URI of the attribute, or <see langword="null"/> or an empty string for an attribute in no namespace. The default is <see langword="null"/>.</param>
        /// <returns>
        /// The attribute that was changed, or the new attribute that was added.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="localName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="localName"/> is empty or consists only of white-space characters.
        /// </exception>
        /// <remarks>
        /// The attribute is found by its local name and its namespace together, so an attribute with the same local
        /// name in another namespace is left as it is. An attribute that this method adds has no prefix. When <paramref name="namespaceUri"/> is
        /// <see langword="null"/>, the new attribute is in no namespace.
        /// </remarks>
        public XmlAttributeNode SetAttribute(string localName, string? value, string? namespaceUri = null)
        {
            // To set an attribute is to name it exactly. With no namespace given, that is the attribute in no
            // namespace, not the first one in any namespace that happens to share the local name.
            var existing = Attributes[localName, namespaceUri ?? string.Empty];
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
