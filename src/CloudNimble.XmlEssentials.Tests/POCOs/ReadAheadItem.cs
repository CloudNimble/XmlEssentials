using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Parent object used to prove a look-ahead skip does not overwrite a parent's decoded attribute.
    /// </summary>
    public sealed class ReadAheadItem
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets an encoded attribute on the parent.
        /// </summary>
        [XmlAttribute]
        public string? A { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the parent overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? A1 { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the parent overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? A2 { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the parent overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? A3 { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the parent overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? A4 { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the parent overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? A5 { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the parent overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? A6 { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the parent overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? A7 { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the parent overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? A8 { get; set; }

        /// <summary>
        /// Gets or sets the child element.
        /// </summary>
        public ReadAheadChild? C { get; set; }

        #endregion

    }

}
