using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type in a namespace with members that are put in no namespace, as the local elements of a schema with
    /// unqualified element form are.
    /// </summary>
    [XmlRoot("root", Namespace = "urn:r")]
    public sealed class UnqualifiedMembers
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a nested value in no namespace.
        /// </summary>
        [XmlElement("child", Namespace = "")]
        public UnqualifiedMembers? Child { get; set; }

        /// <summary>
        /// Gets or sets repeated elements in no namespace.
        /// </summary>
        [XmlElement("item", Namespace = "")]
        public List<int> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets a simple value in no namespace.
        /// </summary>
        [XmlElement("local", Namespace = "")]
        public string? Local { get; set; }

        /// <summary>
        /// Gets or sets a value with no namespace given, which takes the default namespace in scope.
        /// </summary>
        public string? Qualified { get; set; }

        /// <summary>
        /// Gets or sets a typed value in no namespace.
        /// </summary>
        [XmlElement<int>("typed", "")]
        public object? Typed { get; set; }

        /// <summary>
        /// Gets or sets a wrapper in no namespace.
        /// </summary>
        [XmlArray("wrap", Namespace = "")]
        public List<int> Wrapped { get; set; } = [];

        #endregion

    }

}