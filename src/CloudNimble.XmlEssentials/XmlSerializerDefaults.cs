namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Named default sets for <see cref="XmlSerializerOptions"/>.
    /// </summary>
    public enum XmlSerializerDefaults
    {

        /// <summary>
        /// Identity names, write a declaration, <c>xsi:nil</c> for null.
        /// </summary>
        General = 0,

        /// <summary>
        /// CamelCase names and <see cref="XmlSerializerOptions.OmitXmlDeclaration"/> true.
        /// </summary>
        Web = 1,

        /// <summary>
        /// Unknown members are errors, duplicates are errors, comments are disallowed.
        /// </summary>
        Strict = 2,

    }

}
