namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies how a property or field without a mapping attribute is mapped: as a child element or as an XML attribute.
    /// </summary>
    public enum XmlPropertyMapping : byte
    {

        /// <summary>
        /// A child element for each member. This is the default.
        /// </summary>
        Element = 0,

        /// <summary>
        /// An XML attribute for each unannotated simple member. Nested objects stay child elements, and their
        /// unannotated simple members are also attributes (the option applies at every type). Collections stay
        /// child elements. A per-member <c>[XmlElement]</c>, <c>[XmlAttribute]</c>, or <c>[XmlText]</c> takes precedence.
        /// </summary>
        Attribute = 1,

    }

}
