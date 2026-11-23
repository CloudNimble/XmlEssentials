namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// How an unannotated property or field is mapped: child element or XML attribute.
    /// </summary>
    public enum XmlPropertyMapping : byte
    {

        /// <summary>
        /// Write the member as a child element. This is the default.
        /// </summary>
        Element = 0,

        /// <summary>
        /// Write unannotated simple members as XML attributes. Nested objects stay child elements, and their
        /// unannotated simple members are also attributes (the option applies at every type). Collections stay
        /// child elements. Per-member <c>[XmlElement]</c>, <c>[XmlAttribute]</c>, or <c>[XmlText]</c> still wins.
        /// </summary>
        Attribute = 1,

    }

}
