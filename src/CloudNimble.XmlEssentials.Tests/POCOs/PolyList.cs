using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds values of a declared type that may be abstract, as repeated elements.
    /// </summary>
    /// <typeparam name="T">The declared type.</typeparam>
    public sealed class PolyList<T>
        where T : class
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets what is held.
        /// </summary>
        public List<T> Items { get; set; } = [];

        #endregion

    }

}
