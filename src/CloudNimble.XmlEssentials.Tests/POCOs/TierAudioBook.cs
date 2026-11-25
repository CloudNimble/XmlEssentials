namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// The bottom of the hierarchy, listed on <see cref="TierProduct"/> only.
    /// </summary>
    public sealed class TierAudioBook : TierBook
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the length in minutes.
        /// </summary>
        public int Minutes { get; set; }

        #endregion

    }

}
