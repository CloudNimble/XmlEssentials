using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Choices
{

    /// <summary>
    /// A choice with one element name for two types, which is refused.
    /// </summary>
    public sealed class ChoiceOfOneNameTwice
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [XmlElement<ChoiceCircle>("a")]
        [XmlElement<ChoiceSquare>("a")]
        public ChoiceShape? One { get; set; }

        #endregion

    }

}
