namespace CloudNimble.XmlEssentials.Samples.Models
{

    /// <summary>
    /// A person, written as one child element per property.
    /// </summary>
    public sealed class Person
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the person's name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the person's age, in years.
        /// </summary>
        public int Age { get; set; }

        #endregion

    }

}
