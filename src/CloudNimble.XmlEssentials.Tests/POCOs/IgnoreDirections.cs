using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose members are ignored in one direction only: when written, or when read.
    /// </summary>
    public sealed class IgnoreDirections
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a number that is written and never read.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)]
        public int Count { get; set; } = 1;

        /// <summary>
        /// Gets or sets repeated elements that are written and never read.
        /// </summary>
        [XmlElement("Item")]
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)]
        public List<int> Items { get; set; } = [1];

        /// <summary>
        /// Gets or sets a value that is written and read.
        /// </summary>
        public string Normal { get; set; } = "n";

        /// <summary>
        /// Gets or sets an element that is written and never read.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)]
        public string ReadIgnored { get; set; } = "r";

        /// <summary>
        /// Gets or sets an attribute that is written and never read.
        /// </summary>
        [XmlAttribute]
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)]
        public string ReadIgnoredAttribute { get; set; } = "ra";

        /// <summary>
        /// Gets or sets a wrapped list that is written and never read.
        /// </summary>
        [XmlArray]
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)]
        public List<int> Tags { get; set; } = [1];

        /// <summary>
        /// Gets or sets an element that is read and never written.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenWriting)]
        public string WriteIgnored { get; set; } = "w";

        /// <summary>
        /// Gets or sets an attribute that is read and never written.
        /// </summary>
        [XmlAttribute]
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenWriting)]
        public string WriteIgnoredAttribute { get; set; } = "wa";

        #endregion

    }

}
