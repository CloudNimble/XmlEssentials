using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a wrapped list of objects.
    /// </summary>
    public sealed class Team
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the members, inside a <c>Members</c> wrapper.
        /// </summary>
        [XmlArray]
        public List<Person>? Members { get; set; }

        #endregion

    }

}
