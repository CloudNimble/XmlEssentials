using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that maps one member as both an attribute and an element, which is illegal.
    /// </summary>
    public sealed class AttributeAndElement
    {

        #region Public Properties

        /// <summary>
        /// Gets a value annotated as both an attribute and an element.
        /// </summary>
        [XmlAttribute]
        [XmlElement]
        public int N { get; set; }

        #endregion

    }

}
