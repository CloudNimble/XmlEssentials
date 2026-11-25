using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Choices
{

    /// <summary>
    /// A choice with an alternative that is not the member's type, which is refused.
    /// </summary>
    public sealed class ChoiceOfAnotherType
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [XmlElement<ChoiceCircle>("circle")]
        [XmlElement<string>("text")]
        public ChoiceShape? One { get; set; }

        #endregion

    }

}
