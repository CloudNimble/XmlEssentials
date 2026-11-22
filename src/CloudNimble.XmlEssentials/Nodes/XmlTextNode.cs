using System;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// A mutable character-data node.
    /// </summary>
    public sealed class XmlTextNode : XmlNode
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string Value { get; set; }

        /// <inheritdoc />
        public override XmlValueKind ValueKind => XmlValueKind.Text;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a text node.
        /// </summary>
        /// <param name="value">The text.</param>
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
