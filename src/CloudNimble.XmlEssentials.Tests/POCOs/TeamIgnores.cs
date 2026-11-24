using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds wrapped collections that are left out when they are null.
    /// </summary>
    public sealed class TeamIgnores
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a wrapped collection that is ignored when it is the default.
        /// </summary>
        [XmlArray]
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingDefault)]
        public List<Person>? Defaults { get; set; }

        /// <summary>
        /// Gets or sets a wrapped collection that is ignored when it is null.
        /// </summary>
        [XmlArray]
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingNull)]
        public List<Person>? Nulls { get; set; }

        /// <summary>
        /// Gets or sets a wrapped collection that is never ignored.
        /// </summary>
        [XmlArray]
        [XmlIgnore(Condition = XmlIgnoreCondition.Never)]
        public List<Person>? Always { get; set; }

        #endregion

    }

}
