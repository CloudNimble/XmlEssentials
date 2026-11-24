using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Paths
{

    /// <summary>
    /// A line of a <see cref="PathOrder"/>.
    /// </summary>
    public sealed class PathLine
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a label, which the writer refuses when it holds a character XML cannot carry.
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        public int Qty { get; set; }

        /// <summary>
        /// Gets or sets the row number, an attribute.
        /// </summary>
        [XmlAttribute]
        public int Row { get; set; }

        /// <summary>
        /// Gets or sets a tag, an attribute, which the writer refuses when it holds a character XML cannot carry.
        /// </summary>
        [XmlAttribute]
        public string? Tag { get; set; }

        #endregion

    }

}
