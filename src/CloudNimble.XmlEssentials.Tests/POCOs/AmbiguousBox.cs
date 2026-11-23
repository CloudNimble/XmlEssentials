namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with two constructors that each bind a different single member.
    /// </summary>
    public sealed class AmbiguousBox
    {

        #region Public Properties

        /// <summary>
        /// Gets the age.
        /// </summary>
        public int Age { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string? Name { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a box from a name.
        /// </summary>
        /// <param name="Name">The name.</param>
        public AmbiguousBox(string Name)
        {
            this.Name = Name;
        }

        /// <summary>
        /// Creates a box from an age.
        /// </summary>
        /// <param name="Age">The age.</param>
        public AmbiguousBox(int Age)
        {
            this.Age = Age;
        }

        #endregion

    }

}
