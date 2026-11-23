namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A contact with members that can be null: two strings and a nullable number.
    /// </summary>
    public sealed class Contact
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the contact's name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the contact's nickname, or <see langword="null"/> when there is none.
        /// </summary>
        public string? Nickname { get; set; }

        /// <summary>
        /// Gets or sets the contact's age in years, or <see langword="null"/> when it is not known.
        /// </summary>
        public int? Age { get; set; }

        #endregion

    }

}
