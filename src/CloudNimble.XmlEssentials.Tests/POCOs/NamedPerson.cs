using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A person whose name is written as <c>FullName</c>.
    /// </summary>
    public sealed class NamedPerson
    {

        #region Public Properties

        /// <summary>
        /// Gets the given name.
        /// </summary>
        [XmlPropertyName("FullName")]
        public string? Name { get; set; }

        #endregion

    }

}
