using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// The top of a hierarchy three levels deep whose derived types are told apart by their element names.
    /// </summary>
    [XmlDerivedType<NameTierBook>]
    [XmlDerivedType<NameTierAudioBook>]
    public class NameTierProduct
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string? Title { get; set; }

        #endregion

    }

}
