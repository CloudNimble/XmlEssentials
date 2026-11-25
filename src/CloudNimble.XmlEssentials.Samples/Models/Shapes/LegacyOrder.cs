using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// An order whose document element is in a namespace and whose <c>Id</c> element is in none, as a schema with
    /// unqualified local elements describes it.
    /// </summary>
    [XmlRoot("Order", Namespace = "urn:example:orders")]
    public sealed class LegacyOrder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the customer, which takes the namespace of the order.
        /// </summary>
        public string? Customer { get; set; }

        /// <summary>
        /// Gets or sets the identifier, which is in no namespace.
        /// </summary>
        [XmlElement("Id", Namespace = "")]
        public int Id { get; set; }

        #endregion

    }

}