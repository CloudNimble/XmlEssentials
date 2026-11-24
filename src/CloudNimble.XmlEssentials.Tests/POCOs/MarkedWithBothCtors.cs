using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A class with a public parameterless constructor and a marked constructor with parameters, and members with no setter.
    /// </summary>
    public sealed class MarkedWithBothCtors
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
        public MarkedWithBothCtors()
        {
        }

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        [XmlConstructor]
        public MarkedWithBothCtors(string? name, int value)
        {
            Name = name;
            Value = value;
        }

        #endregion

    }

}
