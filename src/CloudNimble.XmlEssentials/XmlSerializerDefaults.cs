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

        /// <summary>
        /// Option values that write what <c>System.Xml.Serialization.XmlSerializer</c> writes for the same
        /// type, as far as the options reach: collections in a wrapper element, <see langword="null"/>
        /// references left out, public fields included and written first, read-only properties left out, collections with no
        /// setter filled, a <see cref="char"/> as its number, <c>Z</c> for a zero offset, two-space
        /// indentation, <c>&lt;e /&gt;</c> for an empty element, and the <c>xsi</c> and <c>xsd</c> namespaces
        /// declared on the document element.
        /// </summary>
        Bcl = 3,

    }

}
