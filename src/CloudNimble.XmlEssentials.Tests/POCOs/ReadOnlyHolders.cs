using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Read-only members of each kind: a dictionary and a value with no setter, and a collection and a value in
    /// <see langword="readonly"/> fields.
    /// </summary>
    public sealed class ReadOnlyHolders
    {

        #region Fields

        /// <summary>
        /// A value in a read-only field.
        /// </summary>
        public readonly int Fixed = 1;

        /// <summary>
        /// A collection in a read-only field.
        /// </summary>
        public readonly List<int> Numbers = [];

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets a value that is worked out.
        /// </summary>
        public string Computed => "c";

        /// <summary>
        /// Gets a dictionary with no setter.
        /// </summary>
        public Dictionary<string, int> Counts { get; } = [];

        #endregion

    }

}