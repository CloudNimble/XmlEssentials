using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A product that writes unknown derived types as the nearest declared ancestor.
    /// </summary>
    [XmlPolymorphic(UnknownDerivedTypeHandling = XmlUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
    [XmlDerivedType<NearBook>]
    public class NearProduct
    {

        #region Public Properties

        /// <summary>
        /// Gets the title.
        /// </summary>
        public string? Title { get; set; }

        #endregion

    }

}
