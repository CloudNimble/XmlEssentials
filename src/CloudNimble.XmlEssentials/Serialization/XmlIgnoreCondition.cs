namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies when a property or field is ignored on serialization and deserialization.
    /// </summary>
    public enum XmlIgnoreCondition
    {

        /// <summary>
        /// A member that is always serialized and deserialized.
        /// </summary>
        Never = 0,

        /// <summary>
        /// A member that is always ignored, on both serialization and deserialization.
        /// </summary>
        Always = 1,

        /// <summary>
        /// A member that is not written when its value is <see langword="null"/> or equals the default value for its type.
        /// </summary>
        WhenWritingDefault = 2,

        /// <summary>
        /// A member that is not written when its value is <see langword="null"/>.
        /// </summary>
        WhenWritingNull = 3,

    }

}
