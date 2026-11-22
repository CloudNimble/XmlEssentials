namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// How objects are created when deserializing into an existing instance.
    /// </summary>
    public enum XmlObjectCreationHandling
    {

        /// <summary>
        /// Replace the existing value.
        /// </summary>
        Replace = 0,

        /// <summary>
        /// Populate the existing value.
        /// </summary>
        Populate = 1,

    }

}
