using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Choices
{

    /// <summary>
    /// Members whose elements are told apart by name, because two of them hold one type.
    /// </summary>
    public sealed class NamedHolder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets one value that is one of two text elements.
        /// </summary>
        [XmlElement<string>("Home")]
        [XmlElement<string>("Work")]
        public XmlNamedValue? Phone { get; set; }

        /// <summary>
        /// Gets or sets values of several elements, in the order they were added.
        /// </summary>
        [XmlElement<string>("k")]
        [XmlElement<string>("v")]
        [XmlElement<int>("n")]
        [XmlElement<TypedShape>("shape")]
        [XmlElement<ChoiceShape>("plain")]
        public List<XmlNamedValue> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets a value whose two elements have one name in two namespaces.
        /// </summary>
        [XmlElement<string>("id", "urn:a")]
        [XmlElement<string>("id", "urn:b")]
        public XmlNamedValue? Spaced { get; set; }

        /// <summary>
        /// Gets or sets items in a wrapper.
        /// </summary>
        [XmlArray("wrapped")]
        [XmlArrayItem<string>("a")]
        [XmlArrayItem<string>("b")]
        public List<XmlNamedValue> Wrapped { get; set; } = [];

        #endregion

    }

}