using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds shapes as the items of a collection in a wrapper, under a default namespace.
    /// </summary>
    [XmlRoot(Namespace = "urn:holder")]
    public sealed class NamespacedShapes
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the shapes, in a wrapper.
        /// </summary>
        [XmlArray]
        public List<NamespacedShape> Shapes { get; set; } = [];

        #endregion

    }

}
