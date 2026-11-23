namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A class with read-only properties and one constructor whose parameters are named in camelCase.
    /// </summary>
    public sealed class CamelCtor
    {

        #region Public Properties

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public double Value { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public CamelCtor(string name, double value)
        {
            Name = name;
            Value = value;
        }

        #endregion

    }

}
