namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Maps objects to reference ids for one serialize or deserialize operation.
    /// </summary>
    public abstract class XmlReferenceResolver
    {

        #region Public Methods

        /// <summary>
        /// Records <paramref name="value"/> under <paramref name="referenceId"/> for later resolve.
        /// </summary>
        /// <param name="referenceId">The id.</param>
        /// <param name="value">The object.</param>
        public abstract void AddReference(string referenceId, object value);

        /// <summary>
        /// Returns the id for <paramref name="value"/> and whether it was already seen.
        /// </summary>
        /// <param name="value">The object.</param>
        /// <param name="alreadyExists">When this method returns, whether the object was already assigned an id.</param>
        /// <returns>The id.</returns>
        public abstract string GetReference(object value, out bool alreadyExists);

        /// <summary>
        /// Returns the object previously recorded under <paramref name="referenceId"/>, or <see langword="null"/>.
        /// </summary>
        /// <param name="referenceId">The id.</param>
        /// <returns>The object, or <see langword="null"/>.</returns>
        public abstract object? ResolveReference(string referenceId);

        #endregion

    }

}
