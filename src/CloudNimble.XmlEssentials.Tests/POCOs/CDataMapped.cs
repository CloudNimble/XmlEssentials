using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose payload is written as a CDATA section.
    /// </summary>
    public sealed class CDataMapped
    {

        #region Public Properties

        /// <summary>
        /// Gets the payload.
        /// </summary>
        [XmlCData]
        public string? Value { get; set; }

        #endregion

    }

}
