using System.Collections.Generic;
using System.Text.Json.Serialization;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// A delivery that is read and written as XML and as JSON. Each serializer reads its own attributes and
    /// ignores the other's.
    /// </summary>
    public sealed class Delivery
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the carrier.
        /// </summary>
        public string? Carrier { get; set; }

        /// <summary>
        /// Gets or sets the delivery number, which XML holds in an attribute and JSON in a property.
        /// </summary>
        [XmlAttribute]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets what is in the delivery: repeated <c>item</c> elements in XML, and an <c>items</c> array in JSON.
        /// </summary>
        [JsonPropertyName("items")]
        [XmlElement("item")]
        public List<string> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets a value that indicates whether the delivery was signed for.
        /// </summary>
        public bool Signed { get; set; }

        /// <summary>
        /// Gets or sets the weight in kilograms.
        /// </summary>
        public decimal Weight { get; set; }

        #endregion

    }

}
