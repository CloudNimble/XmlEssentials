namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies the predefined sets of options that an <see cref="XmlSerializerOptions"/> instance can be created with.
    /// </summary>
    public enum XmlSerializerDefaults
    {

        /// <summary>
        /// General-purpose option values: names are left unchanged, an XML declaration is written, and a
        /// <see langword="null"/> value is written with <c>xsi:nil</c>.
        /// </summary>
        General = 0,

        /// <summary>
        /// Option values for web-based scenarios: names are camel-cased and
        /// <see cref="XmlSerializerOptions.OmitXmlDeclaration"/> is <see langword="true"/>.
        /// </summary>
        Web = 1,

        /// <summary>
        /// Option values for strict deserialization: an unmapped member, a duplicate property, a comment, or a
        /// <see langword="null"/> for a member annotated non-nullable is an error.
        /// </summary>
        Strict = 2,

    }

}
