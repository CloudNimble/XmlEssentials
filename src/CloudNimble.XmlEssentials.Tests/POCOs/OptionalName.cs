namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose constructor parameter has a default value.
    /// </summary>
    public sealed class OptionalName
    {

        #region Public Properties

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="Name">The name.</param>
        public OptionalName(string Name = "anon")
        {
            this.Name = Name;
        }

        #endregion

    }

}
