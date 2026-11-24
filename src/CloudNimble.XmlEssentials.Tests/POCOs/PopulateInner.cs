using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An object with a list and an object of its own, both with no setter.
    /// </summary>
    public sealed class PopulateInner
    {

        #region Public Properties

        /// <summary>
        /// Gets an object.
        /// </summary>
        public PopulateChild Child { get; } = new() { A = 7 };

        /// <summary>
        /// Gets a list.
        /// </summary>
        [XmlArray]
        public List<int> List { get; } = [1];

        /// <summary>
        /// Gets or sets a value.
        /// </summary>
        public int Size { get; set; } = 3;

        #endregion

    }

}
