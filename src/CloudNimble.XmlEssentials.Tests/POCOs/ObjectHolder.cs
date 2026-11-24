using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds members declared as <see cref="object"/>, which are written as the type each value has.
    /// </summary>
    public sealed class ObjectHolder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a single value.
        /// </summary>
        public object? Any { get; set; }

        /// <summary>
        /// Gets or sets a list of values.
        /// </summary>
        public List<object>? Items { get; set; }

        /// <summary>
        /// Gets or sets a dictionary of values.
        /// </summary>
        public Dictionary<string, object?>? Map { get; set; }

        #endregion

    }

}
