using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with one member that sets a positive order and one that sets none.
    /// </summary>
    public sealed class MixedOrder
    {

        #region Public Properties

        /// <summary>
        /// Gets the member with a positive order, written after the member that sets none.
        /// </summary>
        [XmlPropertyOrder(5)]
        public int A { get; set; }

        /// <summary>
        /// Gets the member that sets no order.
        /// </summary>
        public int Z { get; set; }

        #endregion

    }

}
