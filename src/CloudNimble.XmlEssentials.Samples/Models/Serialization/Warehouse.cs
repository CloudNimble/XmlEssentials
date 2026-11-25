using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A warehouse with one attribute and two child elements, used to show naming policies.
    /// </summary>
    public sealed class Warehouse
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the code that identifies the warehouse, written as an attribute.
        /// </summary>
        [XmlAttribute]
        public string? WarehouseCode { get; set; }

        /// <summary>
        /// Gets or sets the street address.
        /// </summary>
        public string? StreetAddress { get; set; }

        /// <summary>
        /// Gets or sets the number of loading docks.
        /// </summary>
        public int LoadingDocks { get; set; }

        #endregion

    }

}
