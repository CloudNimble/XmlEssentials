namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Determines how <see cref="XmlSerializer"/> handles a member that already holds a value when it is deserialized.
    /// </summary>
    /// <remarks>
    /// The values and their meaning are those of <c>System.Text.Json.Serialization.JsonObjectCreationHandling</c>.
    /// </remarks>
    public enum XmlObjectCreationHandling
    {

        /// <summary>
        /// A new instance is created and assigned to the member. A member with no setter is not read.
        /// </summary>
        Replace = 0,

        /// <summary>
        /// The instance the member holds is populated: items are added to a collection, entries to a dictionary, and
        /// the members of an object are set on it. A member that holds <see langword="null"/> is given a new instance
        /// when it has a setter.
        /// </summary>
        Populate = 1,

    }

}
