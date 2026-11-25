using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A person whose secret is never written.
    /// </summary>
    public sealed class IgnoredPerson
    {

        #region Public Properties

        /// <summary>
        /// Gets the given name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets a value that is not serialized.
        /// </summary>
        [XmlIgnore]
        public string? Secret { get; set; }

        #endregion

    }

}
