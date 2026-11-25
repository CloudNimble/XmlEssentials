namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies the version of the XML recommendation that is in force for a document.
    /// </summary>
    /// <remarks>
    /// <see cref="Utf8XmlReader"/> takes the version from the XML declaration. An omitted declaration
    /// means <see cref="Version10"/>. XML 1.1 is not a superset of 1.0: NEL and LS are line endings
    /// only in 1.1, RestrictedChar must be a character reference in 1.1, and <c>xmlns:p=""</c> is
    /// legal only in Namespaces in XML 1.1.
    /// </remarks>
    public enum XmlVersion : byte
    {
        /// <summary>
        /// XML 1.0 Fifth Edition. This is the version when the declaration is omitted or says <c>version="1.0"</c>.
        /// </summary>
        Version10 = 0,

        /// <summary>
        /// XML 1.1 Second Edition. Selected by <c>version="1.1"</c> in the XML declaration.
        /// </summary>
        Version11 = 1,
    }

}
