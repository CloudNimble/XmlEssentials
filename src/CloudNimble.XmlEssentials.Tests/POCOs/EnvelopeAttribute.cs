using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds, as an attribute, a value whose converter writes elements only.
    /// </summary>
    public sealed class EnvelopeAttribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [XmlAttribute]
        public Envelope? Letter { get; set; }

        #endregion

    }

}
