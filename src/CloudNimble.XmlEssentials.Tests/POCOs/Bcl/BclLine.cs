namespace CloudNimble.XmlEssentials.Tests.POCOs.Bcl
{

    /// <summary>
    /// A line of a <see cref="BclOrder"/>.
    /// </summary>
    public class BclLine
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        [CloudNimble.Text.Xml.Serialization.XmlPropertyOrder(1)]
        public int Qty { get; set; }

        /// <summary>
        /// Gets or sets the stock keeping unit.
        /// </summary>
        [CloudNimble.Text.Xml.Serialization.XmlPropertyOrder(0)]
        public string? Sku { get; set; }

        #endregion

    }

}
