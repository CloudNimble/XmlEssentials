using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a dictionary of scores.
    /// </summary>
    public sealed class Scorecard
    {

        #region Public Properties

        /// <summary>
        /// Gets the scores, keyed by name.
        /// </summary>
        public Dictionary<string, int>? Scores { get; set; }

        #endregion

    }

}
