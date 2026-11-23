using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A sign-up with members that must be present in the XML.
    /// </summary>
    public sealed class Registration
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the email address. The C# <see langword="required"/> modifier makes it required in the XML.
        /// </summary>
        public required string Email { get; set; }

        /// <summary>
        /// Gets or sets the plan. <see cref="XmlRequiredAttribute"/> makes it required in the XML.
        /// </summary>
        [XmlRequired]
        public string? Plan { get; set; }

        /// <summary>
        /// Gets or sets who referred the user. It is optional.
        /// </summary>
        public string? Referrer { get; set; }

        #endregion

    }

}
