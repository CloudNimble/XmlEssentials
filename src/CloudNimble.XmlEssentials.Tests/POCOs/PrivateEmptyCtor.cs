namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose only constructor is private and parameterless.
    /// </summary>
    public sealed class PrivateEmptyCtor
    {

        #region Public Properties

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an empty instance.
        /// </summary>
        private PrivateEmptyCtor()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates an instance for tests that serialize a known value.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The instance.</returns>
        public static PrivateEmptyCtor Create(string name)
        {
            return new PrivateEmptyCtor { Name = name };
        }

        #endregion

    }

}
