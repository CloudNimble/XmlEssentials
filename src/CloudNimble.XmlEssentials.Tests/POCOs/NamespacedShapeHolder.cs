using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds a shape as a member, under a default namespace.
    /// </summary>
    [XmlRoot(Namespace = "urn:holder")]
    public sealed class NamespacedShapeHolder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets one shape.
        /// </summary>
        public NamespacedShape? Shape { get; set; }

        #endregion

    }

}
