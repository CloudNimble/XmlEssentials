using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A mixed-content member whose items are not <see cref="XmlMixedContentItem"/>, which is refused.
    /// </summary>
    public sealed class MixedOfObjects
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        [XmlMixed]
        public List<object> Content { get; set; } = [];

        #endregion

    }

}