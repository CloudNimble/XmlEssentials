namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A person with a nested address.
    /// </summary>
    public sealed class Employee
    {

        #region Public Properties

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets the address.
        /// </summary>
        public Address? Home { get; set; }

        #endregion

    }

}
