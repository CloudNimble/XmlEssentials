using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose child is in an explicit namespace.
    /// </summary>
    public sealed class NamespacedChild
    {

        #region Public Properties

        /// <summary>
        /// Gets the payload.
        /// </summary>
        [XmlElement("N", "urn:n")]
        public int N { get; set; }

        #endregion

    }

}
