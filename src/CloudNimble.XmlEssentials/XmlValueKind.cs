namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Discriminator for an <see cref="XmlElement"/> cursor or an <c>XmlNode</c> in the mutable tree.
    /// </summary>
    public enum XmlValueKind : byte
    {

        /// <summary>
        /// No node is selected.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// The document node (immutable <see cref="XmlDocument"/> only).
        /// </summary>
        Document = 1,

        /// <summary>
        /// An element with a start and end tag.
        /// </summary>
        Element = 2,

        /// <summary>
        /// An empty element (<c>&lt;e/&gt;</c>).
        /// </summary>
        EmptyElement = 3,

        /// <summary>
        /// An attribute.
        /// </summary>
        Attribute = 4,

        /// <summary>
        /// Character data.
        /// </summary>
        Text = 5,

        /// <summary>
        /// A CDATA section.
        /// </summary>
        CData = 6,

        /// <summary>
        /// A comment.
        /// </summary>
        Comment = 7,

        /// <summary>
        /// A processing instruction other than the XML declaration.
        /// </summary>
        ProcessingInstruction = 8,

        /// <summary>
        /// The XML declaration.
        /// </summary>
        XmlDeclaration = 9,

        /// <summary>
        /// An element with <c>xsi:nil="true"</c>.
        /// </summary>
        Nil = 10,

    }

}
