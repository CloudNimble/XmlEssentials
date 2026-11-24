using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose text is written and never read.
    /// </summary>
    public sealed class IgnoreDirectionsText
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the text of the element.
        /// </summary>
        [XmlText]
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)]
        public string Body { get; set; } = "b";

        #endregion

    }

}
