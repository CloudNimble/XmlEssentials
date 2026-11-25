using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A scoreboard with a wrapped array that uses the default item name.
    /// </summary>
    public sealed class Scoreboard
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the player.
        /// </summary>
        public string? Player { get; set; }

        /// <summary>
        /// Gets or sets the scores, written inside a <c>Scores</c> element.
        /// </summary>
        [XmlArray]
        public int[]? Scores { get; set; }

        #endregion

    }

}
