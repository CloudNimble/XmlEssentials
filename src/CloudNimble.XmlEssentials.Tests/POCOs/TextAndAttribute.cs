using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A member mapped as both text and an attribute, which is illegal.
    /// </summary>
    public sealed class TextAndAttribute
    {

        #region Public Properties

        /// <summary>
        /// Gets the value.
        /// </summary>
        [XmlText]
        [XmlAttribute]
        public string? Value { get; set; }

        #endregion

    }

}
