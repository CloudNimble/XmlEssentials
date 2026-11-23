using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that illegally combines mixed content with another element member.
    /// </summary>
    public sealed class MixedPlusChild
    {

        #region Public Properties

        /// <summary>
        /// Gets the mixed content.
        /// </summary>
        [XmlMixed]
        public List<XmlMixedContentItem> Content { get; set; } = [];

        /// <summary>
        /// Gets a sibling element, which cannot appear with <see cref="XmlMixedAttribute"/>.
        /// </summary>
        public string? Title { get; set; }

        #endregion

    }

}
