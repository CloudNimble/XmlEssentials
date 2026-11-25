using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Nested object used to prove a look-ahead skip does not overwrite a parent's decoded attribute.
    /// </summary>
    public sealed class ReadAheadChild
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets an encoded attribute on the child.
        /// </summary>
        [XmlAttribute]
        public string? B { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the child overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? B1 { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the child overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? B2 { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the child overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? B3 { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the child overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? B4 { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the child overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? B5 { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the child overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? B6 { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the child overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? B7 { get; set; }

        /// <summary>
        /// Gets or sets extra attributes so the child overflows the inline attribute table.
        /// </summary>
        [XmlAttribute]
        public string? B8 { get; set; }

        #endregion

    }

}
