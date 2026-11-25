using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A contact whose phone numbers are elements of two names that both hold text, so each number names its element.
    /// </summary>
    public sealed class ContactCard
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the phone numbers, in the order the contact prefers them.
        /// </summary>
        [XmlElement<string>("Home")]
        [XmlElement<string>("Work")]
        public List<XmlNamedValue> Phones { get; set; } = [];

        #endregion

    }

}