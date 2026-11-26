using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a member that sets the order zero between two members that set none.
    /// </summary>
    public sealed class ZeroOrdered
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the first member, which sets no order.
        /// </summary>
        public int A { get; set; }

        /// <summary>
        /// Gets or sets the member that sets the order zero.
        /// </summary>
        [XmlPropertyOrder(0)]
        public int B { get; set; }

        /// <summary>
        /// Gets or sets the last member, which sets no order.
        /// </summary>
        public int C { get; set; }

        #endregion

    }

}
