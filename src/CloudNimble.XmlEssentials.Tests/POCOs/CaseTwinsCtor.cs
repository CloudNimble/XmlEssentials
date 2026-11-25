namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A class with two members whose names differ only by case, and a constructor parameter for each.
    /// </summary>
    public sealed class CaseTwinsCtor
    {

        #region Public Properties

        /// <summary>
        /// Gets the value of the upper-case member.
        /// </summary>
        public string Name { get; }

#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        /// Gets the value of the lower-case member.
        /// </summary>
        public string name { get; }
#pragma warning restore IDE1006 // Naming Styles

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="name">The value of the lower-case member.</param>
        /// <param name="Name">The value of the upper-case member.</param>
        public CaseTwinsCtor(string name, string Name)
        {
            this.name = name;
            this.Name = Name;
        }

        #endregion

    }

}
