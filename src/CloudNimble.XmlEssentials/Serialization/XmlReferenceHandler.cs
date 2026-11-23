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
        /// Gets a handler that ignores an object when it is reached again from inside itself during serialization.
        /// </summary>
        /// <value>
        /// A handler that writes an empty element marked <c>xsi:nil="true"</c> in place of an object that is still being
        /// written, which is the object that closes a cycle.
        /// </value>
        /// <remarks>
        /// Only a cycle is replaced. An object that is held in two places without a cycle, such as two members or two items
        /// of a list, is written in full each time, as it is by <c>ReferenceHandler.IgnoreCycles</c> in <c>System.Text.Json</c>.
        /// </remarks>
        public static XmlReferenceHandler IgnoreCycles { get; } = new XmlIgnoreCyclesReferenceHandler();

        /// <summary>
        /// Gets a handler that preserves references by writing identifier attributes on serialization and honoring them on deserialization.
        /// </summary>
        /// <value>
        /// A handler that writes an <c>id</c> attribute on each object and a <c>ref</c> attribute in place of an object that
        /// was already written. Both attributes are in the <c>urn:CloudNimble.Text.Xml:refs</c> namespace.
        /// </value>
        /// <remarks>
        /// A repeated object is replaced wherever it appears: as the document element, as the value of a member, and as an
        /// item of a collection. Value types and collections have no identifier.
        /// </remarks>
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
