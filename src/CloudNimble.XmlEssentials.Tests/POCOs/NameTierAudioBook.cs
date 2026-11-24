namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// The bottom of the hierarchy, listed on <see cref="NameTierProduct"/> only.
    /// </summary>
    public sealed class NameTierAudioBook : NameTierBook
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the length in minutes.
        /// </summary>
        public int Minutes { get; set; }

        #endregion

    }

}
