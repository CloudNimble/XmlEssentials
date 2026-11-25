namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A class with a public parameterless constructor, a public constructor with parameters, and members with no setter.
    /// </summary>
    public sealed class GetOnlyWithBothCtors
    {

        #region Public Properties

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public int Value { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance with no name and no value.
        /// </summary>
        public GetOnlyWithBothCtors()
        {
        }

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public GetOnlyWithBothCtors(string? name, int value)
        {
            Name = name;
            Value = value;
        }

        #endregion

    }

}
