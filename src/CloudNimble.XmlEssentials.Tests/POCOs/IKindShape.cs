using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An interface that is the declared type of KindCircle and KindSquare values.
    /// </summary>
    [XmlPolymorphic(TypeDiscriminatorPropertyName = "kind", DiscriminatorPlacement = XmlDiscriminatorPlacement.Attribute)]
    [XmlDerivedType<KindCircle>("circle")]
    [XmlDerivedType<KindSquare>("square")]
    public interface IKindShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        string? Label { get; set; }

        #endregion

    }

}
