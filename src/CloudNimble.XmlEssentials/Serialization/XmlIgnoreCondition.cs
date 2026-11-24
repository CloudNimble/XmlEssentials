namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies when a property or field is ignored on serialization and deserialization.
    /// </summary>
    /// <remarks>
    /// The values and their meaning are those of <c>System.Text.Json.Serialization.JsonIgnoreCondition</c>.
    /// </remarks>
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

        /// <summary>
        /// A member that is never written and is still read.
        /// </summary>
        WhenWriting = 4,

        /// <summary>
        /// A member that is never read and is still written. A document that gives it is accepted, and what it gives is
        /// skipped. A constructor parameter of the same name is still filled.
        /// </summary>
        WhenReading = 5,

    }

}
