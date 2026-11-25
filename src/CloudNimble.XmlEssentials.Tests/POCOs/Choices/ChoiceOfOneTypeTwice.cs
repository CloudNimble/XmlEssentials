using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Choices
{

    /// <summary>
    /// A choice with two elements for one type, which is refused.
    /// </summary>
    public sealed class ChoiceOfOneTypeTwice
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [XmlElement<ChoiceCircle>("a")]
        [XmlElement<ChoiceCircle>("b")]
        public ChoiceShape? One { get; set; }

        #endregion

    }

}
