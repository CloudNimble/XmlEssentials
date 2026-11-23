using System;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// Represents a mutable XML text node. This class cannot be inherited.
    /// </summary>
    public sealed class XmlTextNode : XmlNode
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the text of this node.
        /// </summary>
        /// <value>
        /// The character data of the node, without escaping.
        /// </value>
        public string Value { get; set; }

        /// <inheritdoc />
        public override XmlValueKind ValueKind => XmlValueKind.Text;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlTextNode"/> class with the specified text.
        /// </summary>
        /// <param name="value">The text of the node. The default is <see langword="null"/>, which is stored as an empty string.</param>
        public XmlTextNode(string? value = null)
        {
            Value = value ?? string.Empty;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override XmlNode DeepClone()
        {
            return new XmlTextNode(Value);
        }

        /// <inheritdoc />
        public override void WriteTo(Utf8XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteStringValue(Value);
        }

        #endregion

    }

}
