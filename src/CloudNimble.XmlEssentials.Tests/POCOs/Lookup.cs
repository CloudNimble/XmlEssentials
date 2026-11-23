using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose dictionary member is declared as an interface.
    /// </summary>
    public sealed class Lookup
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the entries, keyed by name.
        /// </summary>
        public IDictionary<string, int>? Entries { get; set; }

        #endregion

    }

}
