namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Provides the well-known XML namespace URIs that XmlEssentials uses. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// The <c>xml</c> and <c>xmlns</c> prefixes are bound to <see cref="Xml"/> and <see cref="Xmlns"/>
    /// for the whole of every document. Do not bind them to another namespace.
    /// </remarks>
    public static class XmlNamespaces
    {

        #region Fields

        /// <summary>
        /// Represents the namespace of the <c>id</c> and <c>ref</c> attributes that the serializer writes to
        /// preserve object identity: <c>urn:CloudNimble.Text.Xml:refs</c>.
        /// </summary>
        public const string Refs = "urn:CloudNimble.Text.Xml:refs";

        /// <summary>
        /// Represents the namespace bound to the <c>xml</c> prefix:
        /// <c>http://www.w3.org/XML/1998/namespace</c>.
        /// </summary>
        public const string Xml = "http://www.w3.org/XML/1998/namespace";

        /// <summary>
        /// Represents the namespace bound to the <c>xmlns</c> prefix:
        /// <c>http://www.w3.org/2000/xmlns/</c>.
        /// </summary>
        public const string Xmlns = "http://www.w3.org/2000/xmlns/";

        /// <summary>
        /// Represents the XML Schema datatypes namespace, commonly bound to the <c>xsd</c> prefix:
        /// <c>http://www.w3.org/2001/XMLSchema</c>.
        /// </summary>
        public const string Xsd = "http://www.w3.org/2001/XMLSchema";

        /// <summary>
        /// Represents the XML Schema instance namespace, commonly bound to the <c>xsi</c> prefix:
        /// <c>http://www.w3.org/2001/XMLSchema-instance</c>.
        /// </summary>
        public const string Xsi = "http://www.w3.org/2001/XMLSchema-instance";

        #endregion

    }

}
