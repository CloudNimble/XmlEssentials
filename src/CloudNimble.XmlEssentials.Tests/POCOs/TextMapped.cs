using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose payload is the simple content of the element.
    /// </summary>
    public sealed class TextMapped
    {

        #region Public Properties

        /// <summary>
        /// Gets the payload.
        /// </summary>
        [XmlText]
        public string? Value { get; set; }

        #endregion

    }

}
