using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A user profile with members that are left out when they hold no value.
    /// </summary>
    public sealed class UserProfile
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name shown to other users.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the biography. It is left out when it is <see langword="null"/>.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingNull)]
        public string? Bio { get; set; }

        /// <summary>
        /// Gets or sets the number of sign-ins. It is left out when it is zero.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingDefault)]
        public int SignInCount { get; set; }

        /// <summary>
        /// Gets or sets the website. It has no ignore condition of its own.
        /// </summary>
        public string? Website { get; set; }

        #endregion

    }

}
