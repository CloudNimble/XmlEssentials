namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// When a member is skipped during serialize or deserialize.
    /// </summary>
    public enum XmlIgnoreCondition
    {

        /// <summary>
        /// The member is always written and read.
        /// </summary>
        Never = 0,

        /// <summary>
        /// The member is always ignored.
        /// </summary>
        Always = 1,

        /// <summary>
        /// Skip when the value equals the default for its type.
        /// </summary>
        WhenWritingDefault = 2,

        /// <summary>
        /// Skip when the value is null.
        /// </summary>
        WhenWritingNull = 3,

    }

}
