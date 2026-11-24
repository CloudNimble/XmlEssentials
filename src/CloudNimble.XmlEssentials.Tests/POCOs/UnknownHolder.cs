using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose values are declared as <see cref="object"/>, so a document says nothing about their type.
    /// </summary>
    public sealed class UnknownHolder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value of any type.
        /// </summary>
        public object? Any { get; set; }

        /// <summary>
        /// Gets or sets a list of values of any type.
        /// </summary>
        [XmlArray]
        public List<object?> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets a dictionary whose values have any type.
        /// </summary>
        public Dictionary<string, object?> Map { get; set; } = [];

        #endregion

    }

}
