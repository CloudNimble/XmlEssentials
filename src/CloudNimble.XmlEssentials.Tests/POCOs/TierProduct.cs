using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// The top of a hierarchy three levels deep, which lists every type below it.
    /// </summary>
    [XmlPolymorphic(TypeDiscriminatorPropertyName = "kind", DiscriminatorPlacement = XmlDiscriminatorPlacement.Attribute)]
    [XmlDerivedType<TierBook>("book")]
    [XmlDerivedType<TierAudioBook>("audio")]
    [XmlDerivedType<TierDvd>("dvd")]
    public class TierProduct
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string? Title { get; set; }

        #endregion

    }

}
