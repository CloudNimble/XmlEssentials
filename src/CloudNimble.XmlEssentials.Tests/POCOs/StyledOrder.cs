using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with several attributes, a member that is null and a list that is empty: the shapes the layout
    /// settings of the writer apply to.
    /// </summary>
    public sealed class StyledOrder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the code, an attribute.
        /// </summary>
        [XmlAttribute]
        public string Code { get; set; } = "";

        /// <summary>
        /// Gets or sets the number, an attribute.
        /// </summary>
        [XmlAttribute]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the lines, a list in a wrapper element.
        /// </summary>
        [XmlArray]
        public List<string> Lines { get; set; } = [];

        /// <summary>
        /// Gets or sets the note, an element that is nil when it is null.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the region, an attribute.
        /// </summary>
        [XmlAttribute]
        public string Region { get; set; } = "";

        #endregion

    }

}
