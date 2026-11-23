using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// The base type of a catalog entry. Each derived type is written under its own element name.
    /// </summary>
    [XmlPolymorphic]
    [XmlDerivedType<Book>]
    [XmlDerivedType<Dvd>("Disc")]
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
