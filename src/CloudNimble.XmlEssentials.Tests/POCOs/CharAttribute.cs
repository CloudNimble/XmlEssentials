using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds one character as an attribute.
    /// </summary>
    public sealed class CharAttribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the character.
        /// </summary>
        [XmlAttribute]
        public char Mark { get; set; }

        #endregion

    }

}
