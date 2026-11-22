using System;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// A mutable XML attribute.
    /// </summary>
    public sealed class XmlAttributeNode : XmlNode
    {

        #region Fields

        private string _localName;
        private string _namespaceUri;
        private string _prefix;
        private string _value;

        #endregion

        #region Public Properties

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

        /// <summary>
        /// Gets or sets the attribute value.
        /// </summary>
        public string Value
        {
            get => _value;
            set => _value = value ?? string.Empty;
        }

        /// <inheritdoc />
        public override XmlValueKind ValueKind => XmlValueKind.Attribute;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an attribute.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="value">The value.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        /// <param name="prefix">The prefix.</param>
        public XmlAttributeNode(string localName, string? value = null, string? namespaceUri = null, string? prefix = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localName);
            _localName = localName;
            _value = value ?? string.Empty;
            _namespaceUri = namespaceUri ?? string.Empty;
            _prefix = prefix ?? string.Empty;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override XmlNode DeepClone()
        {
            return new XmlAttributeNode(_localName, _value, _namespaceUri, _prefix);
        }

        /// <inheritdoc />
        public override void WriteTo(Utf8XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            if (string.IsNullOrWhiteSpace(_prefix))
            {
                if (string.IsNullOrWhiteSpace(_namespaceUri))
                {
                    writer.WriteAttributeString(_localName, _value);
                }
                else
                {
                    writer.WriteAttributeString(_localName, _namespaceUri, _value);
                }

                return;
            }

            writer.WriteAttributeString(_prefix, _localName, _namespaceUri, _value);
        }

        #endregion

    }

}
