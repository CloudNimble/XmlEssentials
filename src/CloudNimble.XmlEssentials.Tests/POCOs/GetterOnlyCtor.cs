using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type created through its constructor that also has a collection with no setter.
    /// </summary>
    public sealed class GetterOnlyCtor
    {

        #region Public Properties

        /// <summary>
        /// Gets the name, which the constructor sets.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a list that starts with one item.
        /// </summary>
        public List<int> Items { get; } = [5];

        /// <summary>
        /// Gets a list written inside a wrapper element, which starts with one item.
        /// </summary>
        [XmlArray]
        public List<string> Tags { get; } = ["t"];

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="name">The name.</param>
        public GetterOnlyCtor(string name)
        {
            Name = name;
        }

        #endregion

    }

}
