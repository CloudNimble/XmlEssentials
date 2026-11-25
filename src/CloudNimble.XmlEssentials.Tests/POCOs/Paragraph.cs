using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A paragraph of mixed content.
    /// </summary>
    public sealed class Paragraph
    {

        #region Public Properties

        /// <summary>
        /// Gets the mixed content.
        /// </summary>
        [XmlMixed]
        public List<XmlMixedContentItem> Content { get; set; } = [];

        #endregion

    }

}
