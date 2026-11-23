using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with two simple-content members, which is illegal.
    /// </summary>
    public sealed class TwoText
    {

        #region Public Properties

        /// <summary>
        /// Gets the first payload.
        /// </summary>
        [XmlText]
        public string? A { get; set; }

        /// <summary>
        /// Gets the second payload.
        /// </summary>
        [XmlCData]
        public string? B { get; set; }

        #endregion

    }

}
