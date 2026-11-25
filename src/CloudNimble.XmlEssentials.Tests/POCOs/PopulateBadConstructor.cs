using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type created through a constructor with a parameter that marks a member to be populated.
    /// </summary>
    /// <param name="Name">The name.</param>
    public sealed record PopulateBadConstructor(string Name)
    {

        #region Public Properties

        /// <summary>
        /// Gets a list.
        /// </summary>
        [XmlArray]
        [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)]
        public List<int> Items { get; } = [1];

        #endregion

    }

}
