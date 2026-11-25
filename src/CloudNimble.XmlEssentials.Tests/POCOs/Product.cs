using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A polymorphic product.
    /// </summary>
    [XmlDerivedType<Book>]
    [XmlDerivedType<Dvd>]
    public class Product
    {

        #region Public Properties

        /// <summary>
        /// Gets the title.
        /// </summary>
        public string? Title { get; set; }

        #endregion

    }

}
