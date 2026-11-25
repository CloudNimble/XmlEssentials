using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose mixed content already holds an item. Mixed content cannot be populated.
    /// </summary>
    public sealed class PopulateMixed
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the text and elements of the element.
        /// </summary>
        [XmlMixed]
        public List<XmlMixedContentItem> Content { get; set; } = [new XmlMixedText("x")];

        #endregion

    }

}
