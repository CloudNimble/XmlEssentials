using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with one explicitly ordered member and one unordered member.
    /// </summary>
    public sealed class MixedOrder
    {

        #region Public Properties

        /// <summary>
        /// Gets the unordered member.
        /// </summary>
        public int A { get; set; }

        /// <summary>
        /// Gets the ordered member, written first.
        /// </summary>
        [XmlPropertyOrder(5)]
        public int Z { get; set; }

        #endregion

    }

}
