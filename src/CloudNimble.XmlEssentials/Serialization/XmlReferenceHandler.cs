namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Defines how <see cref="XmlSerializer"/> deals with references on serialization and deserialization, and provides a base
    /// for handlers that create an <see cref="XmlReferenceResolver"/> for each operation.
    /// </summary>
    public abstract class XmlReferenceHandler
    {

        #region Public Properties

        /// <summary>
        /// Gets a handler that ignores an object when it is reached again during serialization.
        /// </summary>
        /// <value>
        /// A handler that writes an empty element marked <c>xsi:nil="true"</c> in place of an object that was already written
        /// in the same operation.
        /// </value>
        /// <remarks>
        /// The handler tracks every object written in the operation, so a repeated object is replaced whether or not it forms a cycle.
        /// </remarks>
        public static XmlReferenceHandler IgnoreCycles { get; } = new XmlIgnoreCyclesReferenceHandler();

        /// <summary>
        /// Gets a handler that preserves references by writing identifier attributes on serialization and honoring them on deserialization.
        /// </summary>
        /// <value>
        /// A handler that writes an <c>id</c> attribute on each object and a <c>ref</c> attribute in place of an object that
        /// was already written. Both attributes are in the <c>urn:CloudNimble.Text.Xml:refs</c> namespace.
        /// </value>
        public static XmlReferenceHandler Preserve { get; } = new XmlPreserveReferenceHandler();

        #endregion

        #region Public Methods

        /// <summary>
        /// When overridden in a derived class, returns the <see cref="XmlReferenceResolver"/> used for one serialization or deserialization call.
        /// </summary>
        /// <returns>
        /// The resolver to use for the operation.
        /// </returns>
        public abstract XmlReferenceResolver CreateResolver();

        #endregion

    }

}
