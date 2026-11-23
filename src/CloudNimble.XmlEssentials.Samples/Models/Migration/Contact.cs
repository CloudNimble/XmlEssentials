using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// The <c>Person</c> type of the <c>System.Text.Xml</c> proposal's basic examples: one child element for each property.
    /// </summary>
    [XmlRoot("Person")]
    public sealed class Contact
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the age, in years.
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
