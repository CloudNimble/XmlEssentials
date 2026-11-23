using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A derived type whose member sits between the two base members.
    /// </summary>
    public sealed class OrderedDerived : OrderedBase
    {

        #region Public Properties

        /// <summary>
        /// Gets the second element.
        /// </summary>
        [XmlPropertyOrder(1)]
        public string? B { get; set; }

        #endregion

    }

}
