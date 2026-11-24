using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose derived types are named by <c>xsi:type</c>, one of them with a discriminator that has a colon
    /// in it.
    /// </summary>
    [XmlPolymorphic(DiscriminatorPlacement = XmlDiscriminatorPlacement.XsiType)]
    [XmlDerivedType<QualifiedCircle>("circle")]
    [XmlDerivedType<QualifiedSquare>("q:circle")]
    public class QualifiedShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        public string? Label { get; set; }

        #endregion

    }

}