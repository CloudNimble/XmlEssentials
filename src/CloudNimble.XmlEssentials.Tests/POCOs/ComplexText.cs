using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that illegally maps a complex member as simple content.
    /// </summary>
    public sealed class ComplexText
    {

        #region Public Properties

        /// <summary>
        /// Gets a nested object mapped as text.
        /// </summary>
        [XmlText]
        public Person? Value { get; set; }

        #endregion

    }

}
