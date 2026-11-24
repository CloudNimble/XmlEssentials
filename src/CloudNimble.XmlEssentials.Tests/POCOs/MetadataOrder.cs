using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a member of each kind a source-generated context has to describe: an attribute, an init-only
    /// member, a wrapped list of objects, repeated elements, a dictionary, a value type, and a field.
    /// </summary>
    public sealed class MetadataOrder
    {

        #region Fields

        /// <summary>
        /// A public field, which is serialized only when fields are included.
        /// </summary>
        public int Revision;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets a dictionary with a value-type value.
        /// </summary>
        public Dictionary<string, int> Counts { get; set; } = [];

        /// <summary>
        /// Gets or sets the identifier, which is an XML attribute.
        /// </summary>
        [XmlAttribute]
        public int Id { get; set; }

        /// <summary>
        /// Gets the lines, inside a wrapper, in a list with no setter.
        /// </summary>
        [XmlArray]
        public List<MetadataLine> Lines { get; } = [];

        /// <summary>
        /// Gets the name, which can be set only as the object is created.
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// Gets or sets numbers written as repeated elements.
        /// </summary>
        [XmlElement("N")]
        public List<int> Numbers { get; set; } = [];

        /// <summary>
        /// Gets or sets a value type.
        /// </summary>
        public PopulatePoint Origin { get; set; }

        /// <summary>
        /// Gets or sets a value that is left out when it is null.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingNull)]
        public string? Note { get; set; }

        #endregion

    }

}
