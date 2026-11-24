using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Bcl
{

    /// <summary>
    /// Members that are <see langword="null"/>: references, nullable values, and one of each marked with its
    /// own ignore condition.
    /// </summary>
    public class NullableMembers
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a nullable number written as an attribute.
        /// </summary>
        [XmlAttribute]
        public int? Mark { get; set; }

        /// <summary>
        /// Gets or sets a nested object.
        /// </summary>
        public BclLine? Child { get; set; }

        /// <summary>
        /// Gets or sets a nullable number.
        /// </summary>
        public int? Count { get; set; }

        /// <summary>
        /// Gets or sets a collection whose items may be <see langword="null"/>.
        /// </summary>
        [XmlArray]
        public List<string?> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets a nullable number that carries the condition itself.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingNullReference)]
        public int? MarkedCount { get; set; }

        /// <summary>
        /// Gets or sets text that carries the condition itself.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingNullReference)]
        public string? MarkedText { get; set; }

        /// <summary>
        /// Gets or sets text.
        /// </summary>
        public string? Text { get; set; }

        #endregion

    }

}
