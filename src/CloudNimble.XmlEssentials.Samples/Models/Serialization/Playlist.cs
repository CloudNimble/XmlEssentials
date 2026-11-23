using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A playlist whose tracks are written as repeated elements.
    /// </summary>
    public sealed class Playlist
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the playlist.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the track titles.
        /// </summary>
        public List<string> Tracks { get; set; } = [];

        #endregion

    }

}
