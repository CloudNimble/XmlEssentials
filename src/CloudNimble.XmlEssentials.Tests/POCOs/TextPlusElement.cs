using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with simple content and a child element, which is illegal.
    /// </summary>
    public sealed class TextPlusElement
    {

        #region Public Properties

        /// <summary>
        /// Gets the child.
        /// </summary>
        public int N { get; set; }

        /// <summary>
        /// Gets the payload.
        /// </summary>
        [XmlText]
        public string? Value { get; set; }

        #endregion

    }

}
