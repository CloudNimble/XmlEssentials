using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that <see cref="XmlRootAttribute"/> gives a name that is not an identifier.
    /// </summary>
    [XmlRoot("line-item")]
    public sealed class NamedLine
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a number.
        /// </summary>
        public int N { get; set; }

        #endregion

    }

}
