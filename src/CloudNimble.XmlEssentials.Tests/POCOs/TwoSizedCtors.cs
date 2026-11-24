namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A class with two public constructors that take a different number of parameters, and no parameterless one.
    /// </summary>
    public sealed class TwoSizedCtors
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public int Value { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance with a name.
        /// </summary>
        /// <param name="name">The name.</param>
        public TwoSizedCtors(string? name)
        {
            Name = name;
        }

        /// <summary>
        /// Creates an instance with a name and a value.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public TwoSizedCtors(string? name, int value)
        {
            Name = name;
            Value = value;
        }

        #endregion

    }

}
