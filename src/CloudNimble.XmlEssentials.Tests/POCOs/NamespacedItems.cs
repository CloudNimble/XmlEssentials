using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Collections whose item elements are given a namespace, or none, by <see cref="XmlArrayItemAttribute.Namespace"/>.
    /// </summary>
    public sealed class NamespacedItems
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a collection with no wrapper, where the item is the element of the member.
        /// </summary>
        [XmlArrayItem("i", Namespace = "urn:i")]
        public List<int> Bare { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection whose items are given no namespace of their own.
        /// </summary>
        [XmlArray(Namespace = "urn:w")]
        [XmlArrayItem("i")]
        public List<int> Inherit { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection whose items are put in no namespace.
        /// </summary>
        [XmlArray(Namespace = "urn:w")]
        [XmlArrayItem("i", Namespace = "")]
        public List<int> None { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection whose items have a namespace and whose wrapper has another.
        /// </summary>
        [XmlArray(Namespace = "urn:w")]
        [XmlArrayItem("i", Namespace = "urn:i")]
        public List<int> Own { get; set; } = [];

        /// <summary>
        /// Gets or sets a collection whose items have a namespace and whose wrapper has none, with an item that is null.
        /// </summary>
        [XmlArray]
        [XmlArrayItem("i", Namespace = "urn:i")]
        public List<string?> Plain { get; set; } = [];

        #endregion

    }

}
