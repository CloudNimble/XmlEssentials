using System;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// Represents a mutable XML attribute. This class cannot be inherited.
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
        /// Gets or sets the local name of this attribute.
        /// </summary>
        /// <value>
        /// The local name of the attribute, without a prefix.
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
        /// Gets or sets the namespace URI of this attribute.
        /// </summary>
        /// <value>
        /// The namespace URI of the attribute, or an empty string if the attribute is in no namespace. The
        /// value is never <see langword="null"/>. Setting <see langword="null"/> stores an empty string.
        /// </value>
        public override string? NamespaceUri
        {
            get => _namespaceUri;
            set => _namespaceUri = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the namespace prefix of this attribute.
        /// </summary>
        /// <value>
        /// The prefix of the attribute name, or an empty string if the name has no prefix. Setting
        /// <see langword="null"/> stores an empty string.
        /// </value>
        public string Prefix
        {
            get => _prefix;
            set => _prefix = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the value of this attribute.
        /// </summary>
        /// <value>
        /// The value of the attribute, without escaping. Setting <see langword="null"/> stores an empty string.
        /// </value>
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
        /// Initializes a new instance of the <see cref="XmlAttributeNode"/> class with the specified local name, value, namespace URI, and prefix.
        /// </summary>
        /// <param name="localName">The local name of the attribute, without a prefix.</param>
        /// <param name="value">The value of the attribute. The default is <see langword="null"/>, which is stored as an empty string.</param>
        /// <param name="namespaceUri">The namespace URI of the attribute, or <see langword="null"/> or an empty string for an attribute in no namespace. The default is <see langword="null"/>.</param>
        /// <param name="prefix">The prefix of the attribute name, or <see langword="null"/> or an empty string for no prefix. The default is <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="localName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="localName"/> is empty or consists only of white-space characters.
        /// </exception>
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
