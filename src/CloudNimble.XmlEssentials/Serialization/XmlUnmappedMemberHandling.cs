namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// How unknown elements and attributes are treated on deserialize.
    /// </summary>
    public enum XmlUnmappedMemberHandling : byte
    {

        /// <summary>
        /// Ignore members that are not mapped, unless an extension-data bag captures them.
        /// </summary>
        Skip = 0,

        /// <summary>
        /// Throw <see cref="XmlException"/> when an unmapped member is present.
        /// </summary>
        Disallow = 1,

    }

}
