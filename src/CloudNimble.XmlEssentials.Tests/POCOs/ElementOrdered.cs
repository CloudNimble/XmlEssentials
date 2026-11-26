using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose write order comes from <see cref="XmlElementAttribute.Order"/>.
    /// </summary>
    public sealed class ElementOrdered
    {

        #region Public Properties

        /// <summary>
        /// Gets the unordered member.
        /// </summary>
        public int A { get; set; }

        /// <summary>
        /// Gets the ordered member, written first.
        /// </summary>
        [XmlElement(Order = -1)]
        public int Z { get; set; }

        #endregion

    }

}
