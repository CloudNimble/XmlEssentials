namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Well-known XML namespace URIs used by XmlEssentials.
    /// </summary>
    /// <remarks>
    /// The <c>xml</c> and <c>xmlns</c> prefixes are bound to <see cref="Xml"/> and <see cref="Xmlns"/>
    /// for the lifetime of a document. Do not rebind them.
    /// </remarks>
    public static class XmlNamespaces
    {

        #region Fields

        /// <summary>
        /// The namespace of serializer identity <c>id</c> / <c>ref</c> attributes:
        /// <c>urn:CloudNimble.Text.Xml:refs</c>.
        /// </summary>
        public const string Refs = "urn:CloudNimble.Text.Xml:refs";

        /// <summary>
        /// The namespace bound to the <c>xml</c> prefix:
        /// <c>http://www.w3.org/XML/1998/namespace</c>.
        /// </summary>
        public const string Xml = "http://www.w3.org/XML/1998/namespace";

        /// <summary>
        /// The namespace bound to the <c>xmlns</c> prefix:
        /// <c>http://www.w3.org/2000/xmlns/</c>.
        /// </summary>
        public const string Xmlns = "http://www.w3.org/2000/xmlns/";

        /// <summary>
        /// The XML Schema datatypes namespace, commonly bound to <c>xsd</c>:
        /// <c>http://www.w3.org/2001/XMLSchema</c>.
        /// </summary>
        public const string Xsd = "http://www.w3.org/2001/XMLSchema";

        /// <summary>
        /// The XML Schema instance namespace, commonly bound to <c>xsi</c>:
        /// <c>http://www.w3.org/2001/XMLSchema-instance</c>.
        /// </summary>
        public const string Xsi = "http://www.w3.org/2001/XMLSchema-instance";

        #endregion

    }

}
