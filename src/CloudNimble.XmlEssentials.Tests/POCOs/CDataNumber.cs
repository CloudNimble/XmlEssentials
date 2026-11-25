using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Simple content written as CDATA from a non-string scalar.
    /// </summary>
    public sealed class CDataNumber
    {

        #region Public Properties

        /// <summary>
        /// Gets the value.
        /// </summary>
        [XmlCData]
        public int Value { get; set; }

        #endregion

    }

}
