using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A member whose element name differs only in case from the name of another member's derived type.
    /// </summary>
    public sealed class LowerBookShelf
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the item, which is written as <c>Book</c> when it is a <see cref="Book"/>.
        /// </summary>
        public Product? Item { get; set; }

        /// <summary>
        /// Gets or sets a label, written as <c>book</c>.
        /// </summary>
        [XmlPropertyName("book")]
        public string? Label { get; set; }

        #endregion

    }

}
