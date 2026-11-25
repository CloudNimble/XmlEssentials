using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Prefixes
{

    /// <summary>
    /// A document type whose element is in a namespace, with a member that has no namespace of its own.
    /// </summary>
    [XmlRoot("root", Namespace = "urn:r")]
    public sealed class QualifiedRoot
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets an element with no namespace of its own.
        /// </summary>
        [XmlElement("v")]
        public string? V { get; set; }

        #endregion

    }

}
