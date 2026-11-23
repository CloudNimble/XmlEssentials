using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A base type whose members have explicit order 0 and 2.
    /// </summary>
    public class OrderedBase
    {

        #region Public Properties

        /// <summary>
        /// Gets the first element.
        /// </summary>
        [XmlPropertyOrder(0)]
        public string? A { get; set; }

        /// <summary>
        /// Gets the third element.
        /// </summary>
        [XmlPropertyOrder(2)]
        public string? C { get; set; }

        #endregion

    }

}
