namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies how deserialization handles object creation for a property or field that already holds an instance.
    /// </summary>
    public enum XmlObjectCreationHandling
    {

        /// <summary>
        /// Replacement of the existing value with a new instance.
        /// </summary>
        Replace = 0,

        /// <summary>
        /// Population of the instance that the property or field already holds.
        /// </summary>
        Populate = 1,

    }

}
