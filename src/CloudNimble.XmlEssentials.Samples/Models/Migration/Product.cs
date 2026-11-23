using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// The <c>Product</c> type of the <c>System.Text.Xml</c> proposal's polymorphism example. The element name tells the derived types apart.
    /// </summary>
    [XmlPolymorphic]
    [XmlDerivedType<Book>("Book")]
    [XmlDerivedType<Dvd>("DVD")]
    public class Product
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string? Title { get; set; }

        #endregion

    }

}
