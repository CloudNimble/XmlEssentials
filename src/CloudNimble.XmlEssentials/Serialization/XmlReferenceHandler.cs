namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Creates resolvers that track object identity across a serialize or deserialize operation.
    /// </summary>
    public abstract class XmlReferenceHandler
    {

        #region Public Properties

        /// <summary>
        /// Gets a handler that omits already-seen objects as empty nillable elements.
        /// </summary>
        public static XmlReferenceHandler IgnoreCycles { get; } = new XmlIgnoreCyclesReferenceHandler();

        /// <summary>
        /// Gets a handler that writes <c>id</c> / <c>ref</c> attributes in
        /// <c>urn:CloudNimble.Text.Xml:refs</c>.
        /// </summary>
        public static XmlReferenceHandler Preserve { get; } = new XmlPreserveReferenceHandler();

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a resolver for one serialize or deserialize operation.
        /// </summary>
        /// <returns>A new resolver.</returns>
        public abstract XmlReferenceResolver CreateResolver();

        #endregion

    }

}
