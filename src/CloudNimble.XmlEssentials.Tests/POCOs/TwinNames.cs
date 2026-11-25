using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that maps two members to the same element name.
    /// </summary>
    public sealed class TwinNames
    {

        #region Public Properties

        /// <summary>
        /// Gets the first value.
        /// </summary>
        [XmlPropertyName("X")]
        public int A { get; set; }

        /// <summary>
        /// Gets the second value.
        /// </summary>
        [XmlPropertyName("X")]
        public int B { get; set; }

        #endregion

    }

}
