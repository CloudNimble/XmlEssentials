using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// An account with a member that is not public but is still serialized.
    /// </summary>
    public sealed class Account
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the account name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the internal ledger code. <see cref="XmlIncludeAttribute"/> opts this non-public member in.
        /// </summary>
        [XmlInclude]
        internal string? LedgerCode { get; set; }

        #endregion

    }

}
