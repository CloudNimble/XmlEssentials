using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A person whose document element is <c>Human</c>.
    /// </summary>
    [XmlRoot("Human", Namespace = "urn:people")]
    public sealed class RootedPerson
    {

        #region Public Properties

        /// <summary>
        /// Gets the given name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
