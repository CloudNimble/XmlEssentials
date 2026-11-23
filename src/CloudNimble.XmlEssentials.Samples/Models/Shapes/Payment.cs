using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// The base type of a payment. The derived type is named by a <c>kind</c> attribute.
    /// </summary>
    [XmlPolymorphic(DiscriminatorPlacement = XmlDiscriminatorPlacement.Attribute, TypeDiscriminatorPropertyName = "kind")]
    [XmlDerivedType<CardPayment>("card")]
    [XmlDerivedType<BankTransfer>("transfer")]
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
