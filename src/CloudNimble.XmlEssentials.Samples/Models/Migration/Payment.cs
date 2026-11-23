using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// A payment whose derived types are written with an <c>xsi:type</c> attribute, as <c>System.Xml.Serialization.XmlSerializer</c> writes them.
    /// </summary>
    [XmlPolymorphic(DiscriminatorPlacement = XmlDiscriminatorPlacement.XsiType)]
    [XmlDerivedType<CardPayment>("CardPayment")]
    [XmlDerivedType<BankTransfer>("BankTransfer")]
    public class Payment
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the amount paid.
        /// </summary>
        public decimal Amount { get; set; }

        #endregion

    }

}
