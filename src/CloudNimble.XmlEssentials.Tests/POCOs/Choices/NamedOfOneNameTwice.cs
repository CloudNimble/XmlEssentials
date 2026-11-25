using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Choices
{

    /// <summary>
    /// A member of named values with one element name for two types, which is refused.
    /// </summary>
    public sealed class NamedOfOneNameTwice
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [XmlElement<string>("a")]
        [XmlElement<int>("a")]
        public XmlNamedValue? One { get; set; }

        #endregion

    }

}