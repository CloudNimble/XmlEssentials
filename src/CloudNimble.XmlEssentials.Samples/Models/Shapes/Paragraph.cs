using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A paragraph whose content is text mixed with inline elements.
    /// </summary>
    public sealed class Paragraph
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the text runs and child elements, in document order.
        /// </summary>
        [XmlMixed]
        public List<XmlMixedContentItem> Content { get; set; } = [];

        #endregion

    }

}
