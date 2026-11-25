using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// The <c>Person</c> type of the <c>System.Text.Xml</c> proposal's namespace example, mapped with XmlEssentials attributes.
    /// </summary>
    [XmlRoot("Person", Namespace = "http://example.com/person")]
    public sealed class Employee
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the age, written as the <c>age</c> attribute.
        /// </summary>
        [XmlAttribute("age")]
        public int Age { get; set; }

        /// <summary>
        /// Gets or sets the home address, written as an <c>Address</c> element in the address namespace.
        /// </summary>
        [XmlElement("Address", "http://example.com/address")]
        public Address? HomeAddress { get; set; }

        /// <summary>
        /// Gets or sets the name, written as the <c>FullName</c> element.
        /// </summary>
        [XmlElement("FullName")]
        public string? Name { get; set; }

        #endregion

    }

}
