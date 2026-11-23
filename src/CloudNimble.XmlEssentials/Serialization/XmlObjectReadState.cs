namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// What <see cref="XmlObjectConverter{T}"/> knows about one object while its element is being read.
    /// </summary>
    internal struct XmlObjectReadState
    {

        #region Fields

        /// <summary>
        /// The builders of collection members that gather children until the element ends, one slot per member,
        /// or null when no such member has appeared.
        /// </summary>
        internal object?[]? Builders;

        /// <summary>
        /// The unmapped attributes and elements captured so far, or null.
        /// </summary>
        internal XmlExtensionData? Extra;

        /// <summary>
        /// The instance being filled, or null when the type is created through a constructor with parameters.
        /// </summary>
        internal object? Instance;

        /// <summary>
        /// The values held for the constructor, one slot per member, or null when <see cref="Instance"/> exists.
        /// </summary>
        internal object?[]? Values;

        #endregion

    }

}
