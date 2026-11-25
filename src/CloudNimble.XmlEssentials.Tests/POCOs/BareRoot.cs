using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with <see cref="XmlRootAttribute"/> and no explicit element name.
    /// </summary>
    [XmlRoot]
    public sealed class BareRoot
    {

        #region Public Properties

        /// <summary>
        /// Gets the payload.
        /// </summary>
        [XmlElement]
        public int N { get; set; }

        #endregion

    }

}
