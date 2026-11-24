using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type created through a constructor with a parameter, so none of its members is populated.
    /// </summary>
    /// <param name="Name">The name.</param>
    public sealed record PopulateWithConstructor(string Name)
    {

        #region Public Properties

        /// <summary>
        /// Gets a list.
        /// </summary>
        [XmlArray]
        public List<int> Items { get; } = [1];

        #endregion

    }

}
