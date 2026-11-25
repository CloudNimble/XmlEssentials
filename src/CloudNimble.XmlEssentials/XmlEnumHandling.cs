namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies how enumeration values are written.
    /// </summary>
    public enum XmlEnumHandling : byte
    {

        /// <summary>
        /// The name of the enumeration member. A combination of flags is written as the member names separated by spaces.
        /// </summary>
        Name = 0,

        /// <summary>
        /// The underlying integer value.
        /// </summary>
        Integer = 1,

    }

}
