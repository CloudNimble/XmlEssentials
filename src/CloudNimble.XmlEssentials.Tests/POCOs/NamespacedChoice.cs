using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A typed element choice in a namespace.
    /// </summary>
    public sealed class NamespacedChoice
    {

        #region Public Properties

        /// <summary>
        /// Gets the person.
        /// </summary>
        [XmlElement<Person>("P", "urn:p")]
        public Person? Item { get; set; }

        #endregion

    }

}
