using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that marks mixed content to be populated.
    /// </summary>
    public sealed class PopulateBadMixed
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the text and elements of the element.
        /// </summary>
        [XmlMixed]
        [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)]
        public List<XmlMixedContentItem> Content { get; set; } = [new XmlMixedText("x")];

        #endregion

    }

}
