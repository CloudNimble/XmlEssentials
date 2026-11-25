namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Defines the core behavior of preserving references on serialization and deserialization and provides a base for
    /// resolvers that map objects to reference identifiers during one operation.
    /// </summary>
    public abstract class XmlReferenceResolver
    {

        #region Public Methods

        /// <summary>
        /// When overridden in a derived class, adds an entry to the bag of references by using the specified identifier and value.
        /// </summary>
        /// <param name="referenceId">The identifier of the object.</param>
        /// <param name="value">The object that results from reading the XML element.</param>
        /// <remarks>
        /// The serializer calls this method when it reads an element that carries a reference identifier.
        /// </remarks>
        public abstract void AddReference(string referenceId, object value);

        /// <summary>
        /// When overridden in a derived class, gets the reference identifier of the specified value if one exists; otherwise, assigns a new identifier.
        /// </summary>
        /// <param name="value">The object to get an identifier for.</param>
        /// <param name="alreadyExists">When this method returns, contains <see langword="true"/> if a reference to <paramref name="value"/> already exists; otherwise, <see langword="false"/>. This parameter is treated as uninitialized.</param>
        /// <returns>
        /// The reference identifier for <paramref name="value"/>.
        /// </returns>
        /// <remarks>
        /// The serializer calls this method before it writes an object, to decide whether to write the object or a reference to it.
        /// For a new object, it writes the returned identifier as an <c>id</c> attribute. For an object that already exists, it
        /// writes the identifier as a <c>ref</c> attribute on an empty element. When the identifier is an empty string, it writes
        /// no <c>id</c>, and writes <c>xsi:nil="true"</c> in place of an object that already exists.
        /// </remarks>
        public abstract string GetReference(object value, out bool alreadyExists);

        /// <summary>
        /// When overridden in a derived class, returns the object related to the specified reference identifier.
        /// </summary>
        /// <param name="referenceId">The reference identifier related to the returned object.</param>
        /// <returns>
        /// The object that was added under <paramref name="referenceId"/>, or <see langword="null"/> if there is none.
        /// </returns>
        /// <remarks>
        /// The serializer calls this method when it reads an element that refers to another object by identifier.
        /// </remarks>
        public abstract object? ResolveReference(string referenceId);

        #endregion

    }

}
