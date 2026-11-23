namespace CloudNimble.XmlEssentials.Tests.Aot
{

    /// <summary>
    /// A person mapped as child elements for the AOT smoke.
    /// </summary>
    public sealed class Person
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the age in years.
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// Gets or sets the given name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
