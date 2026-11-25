namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies how <see cref="XmlSerializer"/> handles XML elements and attributes that cannot be mapped to a specific
    /// .NET member when deserializing object types.
    /// </summary>
    /// <remarks>
    /// When the type has a member marked with <see cref="XmlExtensionDataAttribute"/>, or a member marked with
    /// <see cref="XmlMixedAttribute"/> for elements, that member captures the unmapped content and neither value applies.
    /// </remarks>
    public enum XmlUnmappedMemberHandling : byte
    {

        /// <summary>
        /// Silent skipping of any unmapped element or attribute. This is the default behavior.
        /// </summary>
        Skip = 0,

        /// <summary>
        /// Rejection of any unmapped element or attribute with an <see cref="XmlException"/>.
        /// </summary>
        Disallow = 1,

    }

}
