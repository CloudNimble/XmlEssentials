using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A member that names a converter for another type.
    /// </summary>
    public sealed class WrongMemberConverter
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a string that names a converter for numbers.
        /// </summary>
        [XmlConverter<HexInt32Converter>]
        public string? Name { get; set; }

        #endregion

    }

}
