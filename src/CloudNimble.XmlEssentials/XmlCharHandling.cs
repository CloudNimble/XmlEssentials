namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies how a <see cref="char"/> is written and read.
    /// </summary>
    public enum XmlCharHandling : byte
    {

        /// <summary>
        /// The character itself, such as <c>a</c>. This is the default.
        /// </summary>
        Character = 0,

        /// <summary>
        /// The number of the character's UTF-16 code unit, such as <c>97</c> for <c>a</c>. This is what
        /// <c>System.Xml.Serialization.XmlSerializer</c> writes.
        /// </summary>
        Number = 1,

    }

}
