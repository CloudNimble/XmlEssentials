using System;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// Represents a mutable XML comment. This class cannot be inherited.
    /// </summary>
    public sealed class XmlCommentNode : XmlNode
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the text of this comment.
        /// </summary>
        /// <value>
        /// The text between <c>&lt;!--</c> and <c>--&gt;</c>.
        /// </value>
        public string Value { get; set; }

        /// <inheritdoc />
        public override XmlValueKind ValueKind => XmlValueKind.Comment;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlCommentNode"/> class with the specified text.
        /// </summary>
        /// <param name="value">The text of the comment. The default is <see langword="null"/>, which is stored as an empty string.</param>
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
