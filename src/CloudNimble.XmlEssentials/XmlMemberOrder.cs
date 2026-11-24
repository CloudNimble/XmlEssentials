namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies where the fields of a type come among its members, when
    /// <see cref="XmlSerializerOptions.IncludeFields"/> includes them.
    /// </summary>
    /// <remarks>
    /// Under either value, the members of a base class come before those of a class derived from it, the members
    /// of one kind keep the order they are declared in, and an explicit order on a member takes precedence.
    /// </remarks>
    public enum XmlMemberOrder : byte
    {

        /// <summary>
        /// The properties of a class, then its fields. This is the default.
        /// </summary>
        PropertiesFirst = 0,

        /// <summary>
        /// The fields of a class, then its properties. This is the order
        /// <c>System.Xml.Serialization.XmlSerializer</c> writes.
        /// </summary>
        FieldsFirst = 1,

    }

}
