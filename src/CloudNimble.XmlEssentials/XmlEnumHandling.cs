namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// How enum values are written.
    /// </summary>
    public enum XmlEnumHandling : byte
    {

        /// <summary>
        /// Write the enum member name. Flags are space-separated names.
        /// </summary>
        Name = 0,

        /// <summary>
        /// Write the underlying integer.
        /// </summary>
        Integer = 1,

    }

}
