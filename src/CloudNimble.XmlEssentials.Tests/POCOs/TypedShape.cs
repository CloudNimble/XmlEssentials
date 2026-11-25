using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An abstract class that is the declared type of TypedCircle and TypedSquare values.
    /// </summary>
    [XmlPolymorphic(DiscriminatorPlacement = XmlDiscriminatorPlacement.XsiType)]
    [XmlDerivedType<TypedCircle>("circle")]
    [XmlDerivedType<TypedSquare>("square")]
    public abstract class TypedShape : ITypedShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        public string? Label { get; set; }

        #endregion

    }

}
