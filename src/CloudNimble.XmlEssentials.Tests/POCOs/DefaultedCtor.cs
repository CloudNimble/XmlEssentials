using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose constructor has a parameter that is not a member and has a default value.
    /// </summary>
    public sealed class DefaultedCtor
    {

        #region Public Properties

        /// <summary>
        /// Gets the name, set by the constructor.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the value the constructor was given for the parameter that is not a member.
        /// </summary>
        public int Total { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="Name">The name.</param>
        /// <param name="bonus">A value that no member maps to.</param>
        [XmlConstructor]
        public DefaultedCtor(string Name, int bonus = 5)
        {
            this.Name = Name;
            Total = bonus;
        }

        #endregion

    }

}
