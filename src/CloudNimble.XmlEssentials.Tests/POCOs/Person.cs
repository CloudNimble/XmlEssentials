namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A person with a name and age, mapped as child elements.
    /// </summary>
    public sealed class Person
    {

        #region Public Properties

        /// <summary>
        /// Gets the given name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets the age in years.
        /// </summary>
        public int Age { get; set; }

        #endregion

    }

}
