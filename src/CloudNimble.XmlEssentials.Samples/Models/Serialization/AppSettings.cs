using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// Application settings stored in a dictionary.
    /// </summary>
    public sealed class AppSettings
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        public string? Application { get; set; }

        /// <summary>
        /// Gets or sets the setting values, by name.
        /// </summary>
        public Dictionary<string, string> Values { get; set; } = [];

        #endregion

    }

}
