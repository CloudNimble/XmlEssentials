namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies the kind of XML node that an <see cref="XmlElement"/> or a
    /// <see cref="CloudNimble.Text.Xml.Nodes.XmlNode"/> represents.
    /// </summary>
    public enum XmlValueKind : byte
    {

        /// <summary>
        /// No node. This is the kind of a default <see cref="XmlElement"/>.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// A document node. Neither <see cref="XmlDocument"/> nor the mutable tree reports this kind.
        /// </summary>
        Document = 1,

        /// <summary>
        /// An element with a start tag and an end tag.
        /// </summary>
        Element = 2,

        /// <summary>
        /// An empty-element tag (<c>&lt;e/&gt;</c>).
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
        /// The XML declaration. Only <see cref="CloudNimble.Text.Xml.Nodes.XmlDeclarationNode"/> reports this kind.
        /// </summary>
        XmlDeclaration = 9,

        /// <summary>
        /// An element whose <c>xsi:nil</c> attribute is <c>true</c> or <c>1</c>. Only <see cref="XmlElement"/> reports this kind.
        /// </summary>
        Nil = 10,

    }

}
