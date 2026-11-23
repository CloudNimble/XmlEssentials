using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// An album whose tracks are written inside a wrapper element.
    /// </summary>
    public sealed class Album
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the title of the album.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the track titles, written as <c>Track</c> elements inside a <c>Tracks</c> element.
        /// </summary>
        [XmlArray("Tracks")]
        [XmlArrayItem("Track")]
        public List<string>? Tracks { get; set; }

        #endregion

    }

}
