using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An interface that is the declared type of TypedCircle and TypedSquare values.
    /// </summary>
    [XmlPolymorphic(DiscriminatorPlacement = XmlDiscriminatorPlacement.XsiType)]
    [XmlDerivedType<TypedCircle>("circle")]
    [XmlDerivedType<TypedSquare>("square")]
    public interface ITypedShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        string? Label { get; set; }

        #endregion

    }

}
