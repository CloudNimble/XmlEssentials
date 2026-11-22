using System;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// A mutable comment node.
    /// </summary>
    public sealed class XmlCommentNode : XmlNode
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the comment text.
        /// </summary>
        public string Value { get; set; }

        /// <inheritdoc />
        public override XmlValueKind ValueKind => XmlValueKind.Comment;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a comment node.
        /// </summary>
        /// <param name="value">The comment text.</param>
        public XmlCommentNode(string? value = null)
        {
            Value = value ?? string.Empty;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override XmlNode DeepClone()
        {
            return new XmlCommentNode(Value);
        }

        /// <inheritdoc />
        public override void WriteTo(Utf8XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteComment(Value);
        }

        #endregion

    }

}
