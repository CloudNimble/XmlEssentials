using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A user account with a member that is never serialized.
    /// </summary>
    public sealed class UserAccount
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the sign-in name.
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Gets or sets the password. It is never written or read.
        /// </summary>
        [XmlIgnore]
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string? Email { get; set; }

        #endregion

    }

}
